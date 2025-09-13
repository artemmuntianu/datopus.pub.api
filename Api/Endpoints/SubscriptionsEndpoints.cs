using datopus.Api.DTOs.Subscriptions;
using datopus.Api.EndpointFilters;
using datopus.Api.Utilities.Auth;
using datopus.Api.Utilities.HttpContextHelpers;
using datopus.Application.Services;
using datopus.Application.Services.Subscriptions;
using datopus.Core.Enums.Constants;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using Stripe.Tax;

namespace datopus.payments.Api.Endpoints
{
    public static class SubscriptionEndpoints
    {
        private static readonly string DOMAIN_BASE_URL = Environment.GetEnvironmentVariable(
            "DOMAIN_WEB_BASE_URL"
        )!;

        private static readonly string STRIPE_WEBHOOK_KEY = Environment.GetEnvironmentVariable(
            "STRIPE_WEBHOOK_KEY"
        )!;

        public static void Register(WebApplication app)
        {
            var endpoints = app.MapGroup("/subscriptions").RequireAuthorization();
            endpoints.MapGet("/", GetSubscriptionById);
            endpoints.MapGet("/plans", GetPlans);
            endpoints.MapGet("/plans/{productId}", GetPlanById);
            endpoints.MapGet("/checkout-session/verify", VerifyCheckout);
            endpoints
                .MapPost("/checkout-session", CreateCheckoutSession)
                .AddEndpointFilter<InputValidatorFilter<CheckoutRequest>>();
            endpoints
                .MapPost("/portal-session", CreatePortalSession)
                .AddEndpointFilter<InputValidatorFilter<PortalSessionRequest>>();

            endpoints
                .MapPost("/change-plan", ChangeSubscriptionPlan)
                .AddEndpointFilter<InputValidatorFilter<ChangeSubscriptionRequest>>();
            endpoints
                .MapPost("/cancel", CancelSubscription)
                .AddEndpointFilter<InputValidatorFilter<CancelSubscriptionRequest>>();
            endpoints
                .MapPost("/estimate-taxes", EsmitateTaxes)
                .AddEndpointFilter<InputValidatorFilter<TaxEstimateRequest>>();
            endpoints
                .MapPost("/prorated-estimate", GetProratedEstimate)
                .AddEndpointFilter<InputValidatorFilter<ProratesEstimateRequest>>();
            endpoints
                .MapPost("/envoice-preview", GetInvoicePreview)
                .AddEndpointFilter<InputValidatorFilter<InvoicePreviewRequest>>();
            endpoints.MapPost("webhook", HandleSubscriptionUpdate).AllowAnonymous();
        }

        private static async Task<
            Results<Ok<SubscriptionResponse>, ProblemHttpResult>
        > GetSubscriptionById(HttpContext context, SubscriptionService subscriptionService)
        {
            var userClaims = ClaimsMapper.MapUserClaims(context);

            if (userClaims?.AppMetaDataClaims?.Role != UserRoles.HomeAdmin)
            {
                return TypedResults.Problem(
                    detail: "The user has no role access to process this request.",
                    statusCode: StatusCodes.Status403Forbidden
                );
            }

            if (userClaims.AppMetaDataClaims.SubscriptionId == null)
            {
                return TypedResults.Problem(
                    detail: "The user has no subscription id set to process this request.",
                    statusCode: StatusCodes.Status403Forbidden
                );
            }

            try
            {
                var subscription = await subscriptionService.GetAsync(
                    userClaims.AppMetaDataClaims.SubscriptionId,
                    new SubscriptionGetOptions { Expand = new List<string>(["items.data.price"]) }
                );

                var lineItem = subscription.Items.LastOrDefault();

                var orgIdString = subscription.Metadata.GetValueOrDefault("orgId");

                if (!long.TryParse(orgIdString, out var orgId))
                {
                    throw new Exception("Unable to get org id from subscription metadata");
                }

                if (orgId != userClaims.AppMetaDataClaims.OrgId)
                {
                    return TypedResults.Problem(
                        "Invalid org id",
                        statusCode: StatusCodes.Status403Forbidden
                    );
                }
                // TODO: match up start and end dates
                var sub = new SubscriptionResponse
                {
                    CancelAtPeriodEnd = subscription.CancelAtPeriodEnd,
                    LatestInvoicePaid = subscription.LatestInvoice.AmountPaid,
                    Status = subscription.Status,
                    OrgId = orgId,
                    CanceledAt = subscription.CancelAt,
                    CreatedAt = subscription.Created,
                    StartDate = subscription.StartDate,
                    EndDate = subscription.CurrentPeriodEnd,
                    StripeSubscriptionId = subscription.Id,
                    Currency = subscription.Currency,
                    PriceId = lineItem?.Price.Id,
                    ProductId = lineItem?.Price.ProductId,
                    StripeCustomerId = subscription.CustomerId,
                    Quantity = lineItem?.Quantity,
                };

                return TypedResults.Ok(sub);
            }
            catch (StripeException ex)
            {
                return TypedResults.Problem(
                    detail: $"Stripe API error: {ex.Message}",
                    statusCode: (int)ex.HttpStatusCode
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex}");
                return TypedResults.Problem(
                    detail: "An unexpected error occurred while fetching subscription.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }

        public static async Task<
            Results<Ok<SessionResponse>, ProblemHttpResult>
        > CreateCheckoutSession(
            HttpContext httpContext,
            CheckoutRequest request,
            [FromServices] SubscriptionDBService subscriptionDBService,
            [FromServices] SubscriptionService subscriptionService,
            [FromServices] PriceService priceService,
            [FromServices] SessionService sessionService
        )
        {
            var userClaims = ClaimsMapper.MapUserClaims(httpContext);

            if (userClaims?.AppMetaDataClaims?.Role != UserRoles.HomeAdmin)
            {
                return TypedResults.Problem(
                    detail: "The user has no role access to process this request.",
                    statusCode: StatusCodes.Status403Forbidden
                );
            }

            if (userClaims?.AppMetaDataClaims?.OrgId == null)
            {
                return TypedResults.Problem(
                    detail: "The user organization is not found.",
                    statusCode: StatusCodes.Status400BadRequest
                );
            }

            var existingSubscription = await subscriptionDBService.GetSubscriptionByOrgId(
                userClaims.AppMetaDataClaims.OrgId!
            );

            if (existingSubscription?.StripeCustomerId != null)
            {
                var stripeSubscription = await subscriptionService.GetAsync(
                    existingSubscription.StripeSubscriptionId
                );

                if (
                    stripeSubscription.Status == SubscriptionStatuses.Active
                    || stripeSubscription.Status == SubscriptionStatuses.Paused
                    || stripeSubscription.Status == SubscriptionStatuses.PastDue
                    || stripeSubscription.Status == SubscriptionStatuses.Unpaid
                )
                {
                    return TypedResults.Problem(
                        detail: $"User already has an pending subscription with status: {stripeSubscription.Status}.",
                        statusCode: StatusCodes.Status400BadRequest
                    );
                }
            }

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                Currency = request.Currency,
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Price = request.PriceId,
                        Quantity = request.Quantity,
                    },
                },
                Mode = "subscription",
                SuccessUrl =
                    $"{DOMAIN_BASE_URL}/{request.SuccessPath}?session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"{DOMAIN_BASE_URL}/{request.CancelPath}",
                Customer = userClaims.AppMetaDataClaims.SubscriptionCustomerId ?? null,
                SubscriptionData = new SessionSubscriptionDataOptions
                {
                    BillingCycleAnchor =
                        subscriptionDBService.GetFirstDayOfTheNextMonthUTCSeconds(),
                    Metadata = new Dictionary<string, string>
                    {
                        { "orgId", userClaims.AppMetaDataClaims.OrgId.ToString() },
                    },
                },
                Metadata = new Dictionary<string, string> { { "userId", userClaims.UserId! } },
            };

            try
            {
                var session = await sessionService.CreateAsync(options);
                return TypedResults.Ok(new SessionResponse(session.Id));
            }
            catch (StripeException ex)
            {
                return TypedResults.Problem(detail: ex.Message, statusCode: (int)ex.HttpStatusCode);
            }
            catch
            {
                return TypedResults.Problem(
                    detail: "An unexpected error occurred while creating checkout session.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }

        public static async Task<Results<Ok, ProblemHttpResult>> ChangeSubscriptionPlan(
            HttpContext httpContext,
            ChangeSubscriptionRequest request,
            SubscriptionDBService subscriptionDBService,
            SubscriptionService subscriptionService
        )
        {
            var userClaims = ClaimsMapper.MapUserClaims(httpContext);

            if (userClaims?.AppMetaDataClaims?.Role != UserRoles.HomeAdmin)
            {
                return TypedResults.Problem(
                    detail: "The user has no role access to process this request.",
                    statusCode: StatusCodes.Status403Forbidden
                );
            }

            var existingSubscription = await subscriptionDBService.GetSubscriptionByOrgId(
                userClaims.AppMetaDataClaims.OrgId
            );

            if (
                existingSubscription == null
                || existingSubscription.StripeSubscriptionId != request.SubscriptionId
            )
            {
                return TypedResults.Problem(
                    detail: "The subscription does not belong to your organization.",
                    statusCode: StatusCodes.Status403Forbidden
                );
            }

            try
            {
                var subscription = await subscriptionService.GetAsync(request.SubscriptionId);

                if (subscription == null)
                {
                    return TypedResults.Problem(
                        detail: "Subscription not found.",
                        statusCode: StatusCodes.Status404NotFound
                    );
                }

                if (
                    subscription.Status != SubscriptionStatuses.Active
                    && subscription.Status != SubscriptionStatuses.PastDue
                    && subscription.Status != SubscriptionStatuses.Paused
                    && subscription.Status != SubscriptionStatuses.Unpaid
                )
                {
                    return TypedResults.Problem(
                        detail: "User without active subscription can't change the plan.",
                        statusCode: StatusCodes.Status400BadRequest
                    );
                }

                if (subscription.Items?.Data == null || !subscription.Items.Data.Any())
                {
                    return TypedResults.Problem(
                        detail: "Subscription has no items to update.",
                        statusCode: StatusCodes.Status400BadRequest
                    );
                }

                var options = new SubscriptionUpdateOptions
                {
                    Items = new List<SubscriptionItemOptions>
                    {
                        new SubscriptionItemOptions
                        {
                            Id = subscription.Items.Data[0].Id,
                            Price = request.PriceId,
                            Quantity = request.Quantity,
                        },
                    },
                    BillingCycleAnchor = SubscriptionBillingCycleAnchor.Unchanged,
                    ProrationBehavior = "always_invoice",
                };

                var updatedSubscription = await subscriptionService.UpdateAsync(
                    subscription.Id,
                    options
                );

                await subscriptionDBService.UpsertSubscriptionAsync(updatedSubscription);

                return TypedResults.Ok();
            }
            catch (StripeException ex)
            {
                return TypedResults.Problem(
                    detail: $"Invalid request to Stripe: {ex.Message}",
                    statusCode: (int)ex.HttpStatusCode
                );
            }
            catch
            {
                return TypedResults.Problem(
                    detail: "An unexpected error occurred while updating the subscription.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }

        public static async Task<
            Results<Ok<SubscriptionCancelResponse>, ProblemHttpResult>
        > CancelSubscription(
            HttpContext httpContext,
            CancelSubscriptionRequest request,
            [FromServices] SubscriptionService subscriptionService,
            [FromServices] SubscriptionDBService subscriptionDBService
        )
        {
            try
            {
                var userClaims = ClaimsMapper.MapUserClaims(httpContext);

                if (userClaims?.AppMetaDataClaims?.Role != UserRoles.HomeAdmin)
                {
                    return TypedResults.Problem(
                        detail: "User does not have permission to cancel subscriptions.",
                        statusCode: StatusCodes.Status403Forbidden
                    );
                }

                var orgSubscription = await subscriptionDBService.GetSubscriptionByOrgId(
                    userClaims.AppMetaDataClaims.OrgId
                );

                if (
                    orgSubscription == null
                    || orgSubscription.StripeSubscriptionId != request.SubscriptionId
                )
                {
                    return TypedResults.Problem(
                        detail: "User is not authorized to cancel this subscription.",
                        statusCode: StatusCodes.Status403Forbidden
                    );
                }

                var subscription = await subscriptionService.GetAsync(request.SubscriptionId);

                if (subscription == null)
                {
                    return TypedResults.Problem(
                        detail: "Subscription not found.",
                        statusCode: StatusCodes.Status404NotFound
                    );
                }

                if (subscription.Status == "canceled")
                {
                    return TypedResults.Problem(
                        detail: "Subscription is already canceled.",
                        statusCode: StatusCodes.Status400BadRequest
                    );
                }

                var cancelOptions = new SubscriptionUpdateOptions { CancelAtPeriodEnd = true };
                var updatedSubscription = await subscriptionService.UpdateAsync(
                    subscription.Id,
                    cancelOptions
                );

                return TypedResults.Ok(
                    new SubscriptionCancelResponse
                    {
                        Message = "Subscription cancellation requested successfully.",
                        SubscriptionId = updatedSubscription.Id,
                        Status = updatedSubscription.Status,
                        CancelAt = updatedSubscription.CancelAt,
                    }
                );
            }
            catch (StripeException ex)
            {
                return TypedResults.Problem(
                    detail: $"Stripe error: {ex.Message}",
                    statusCode: (int)ex.HttpStatusCode
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex}");
                return TypedResults.Problem(
                    detail: "An unexpected error occurred while canceling the subscription.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }

        public static async Task<
            Results<Ok<VerifyCheckoutResponse>, ProblemHttpResult>
        > VerifyCheckout(
            HttpContext httpContext,
            [FromQuery] string session_id,
            [FromServices] SubscriptionDBService subscriptionDBService,
            [FromServices] SessionService sessionService
        )
        {
            try
            {
                var userClaims = ClaimsMapper.MapUserClaims(httpContext);

                if (userClaims?.AppMetaDataClaims?.Role != UserRoles.HomeAdmin)
                {
                    return TypedResults.Problem(
                        detail: "User does not have permission to process checkouts.",
                        statusCode: StatusCodes.Status403Forbidden
                    );
                }

                var session = await sessionService.GetAsync(
                    session_id,
                    new SessionGetOptions
                    {
                        Expand = new List<string>
                        {
                            "line_items",
                            "subscription",
                            "line_items.data.price.product",
                        },
                    }
                );

                if (session == null)
                {
                    return TypedResults.Problem(
                        detail: "Session not found.",
                        statusCode: StatusCodes.Status404NotFound
                    );
                }

                if (session.Metadata.GetValueOrDefault("userId") != userClaims.UserId)
                {
                    return TypedResults.Problem(
                        detail: "Session not found.",
                        statusCode: StatusCodes.Status404NotFound
                    );
                }

                if (session.PaymentStatus != "paid")
                {
                    return TypedResults.Problem(
                        detail: "Payment not verified yet.",
                        statusCode: StatusCodes.Status400BadRequest
                    );
                }

                if (session.Subscription == null)
                {
                    return TypedResults.Problem(
                        detail: "No subscription found in the session.",
                        statusCode: StatusCodes.Status400BadRequest
                    );
                }

                await subscriptionDBService.UpsertSubscriptionAsync(session.Subscription);

                return TypedResults.Ok(
                    new VerifyCheckoutResponse
                    {
                        Message = "Payment verified and subscription details updated successfully.",
                        SubscriptionId = session.Subscription.Id,
                        Status = session.Subscription.Status,
                    }
                );
            }
            catch (StripeException ex)
            {
                return TypedResults.Problem(
                    detail: $"Stripe error: {ex.Message}",
                    statusCode: (int)ex.HttpStatusCode
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex}");
                return TypedResults.Problem(
                    detail: "An unexpected error occurred while verifying checkout. Please contact the administrator.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }

        public static async Task<Results<Ok, ProblemHttpResult>> HandleSubscriptionUpdate(
            HttpContext httpContext,
            SubscriptionDBService subscriptionPriceService
        )
        {
            var json = await new StreamReader(httpContext.Request.Body).ReadToEndAsync();

            try
            {
                var signatureHeader = httpContext.Request.Headers["Stripe-Signature"];

                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    signatureHeader,
                    STRIPE_WEBHOOK_KEY
                );

                if (stripeEvent.Data.Object is Subscription subscription)
                {
                    await subscriptionPriceService.UpsertSubscriptionAsync(subscription);
                }
                else
                {
                    Console.WriteLine($"Unhandled event type: {stripeEvent.Type}");
                }

                return TypedResults.Ok();
            }
            catch (StripeException e)
            {
                Console.WriteLine($"Stripe webhook error: {e.Message}");
                return TypedResults.Problem();
            }
        }

        public static async Task<Results<Ok<TaxEstimateResponse>, ProblemHttpResult>> EsmitateTaxes(
            HttpContext httpContext,
            TaxEstimateRequest request,
            CalculationService calculationService
        )
        {
            var userClaims = ClaimsMapper.MapUserClaims(httpContext);

            if (userClaims?.AppMetaDataClaims?.Role != UserRoles.HomeAdmin)
            {
                return TypedResults.Problem(
                    detail: "User does not have permission to process estimates.",
                    statusCode: StatusCodes.Status403Forbidden
                );
            }

            try
            {
                var address = HttpContextHelper.GetClientIp(httpContext);
                var calculation = await calculationService.CreateAsync(
                    new CalculationCreateOptions
                    {
                        Currency = request.Currency,

                        CustomerDetails = new CalculationCustomerDetailsOptions
                        {
                            IpAddress = address,
                        },
                        LineItems = new List<CalculationLineItemOptions>
                        {
                            new CalculationLineItemOptions
                            {
                                Quantity = request.Quantity,
                                Product = request.ProductId,
                                Reference = request.ProductId,
                            },
                        },
                    }
                );
                return TypedResults.Ok(
                    new TaxEstimateResponse
                    {
                        SubTotalAmount = calculation.AmountTotal - calculation.TaxAmountExclusive,
                        TotalAmount = calculation.AmountTotal,
                        TaxAmountExclusive = calculation.TaxAmountExclusive,
                        TaxAmountInclusive = calculation.TaxAmountInclusive,
                    }
                );
            }
            catch (StripeException ex)
            {
                return TypedResults.Problem(
                    detail: $"Stripe error: {ex.Message}",
                    statusCode: (int)ex.HttpStatusCode
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex}");
                return TypedResults.Problem(
                    detail: "An unexpected error occurred while estimating taxes.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }

        public static async Task<Results<Ok<PlanResponse>, ProblemHttpResult>> GetPlanById(
            string productId,
            IPlanService planService
        )
        {
            try
            {
                var plan = await planService.GetPlanByIdAsync(productId);

                return TypedResults.Ok(plan);
            }
            catch (StripeException ex)
            {
                return TypedResults.Problem(
                    detail: $"Stripe API error: {ex.Message}",
                    statusCode: (int)ex.HttpStatusCode
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return TypedResults.Problem("Unable to fetch plan.");
            }
        }

        public static async Task<
            Results<Ok<IEnumerable<PlanResponse>>, ProblemHttpResult>
        > GetPlans([FromServices] IPlanService planService)
        {
            try
            {
                var plans = await planService.GetPlansAsync();

                return TypedResults.Ok(plans);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return TypedResults.Problem("Unable to get plans.");
            }
        }

        public static async Task<
            Results<Ok<EstimateResponse>, ProblemHttpResult>
        > GetProratedEstimate(
            [FromBody] ProratesEstimateRequest request,
            [FromServices] SubscriptionService subscriptionService,
            [FromServices] SubscriptionDBService subscriptionDBService,
            [FromServices] InvoiceService upcomingInvoiceService,
            HttpContext httpContext
        )
        {
            var userClaims = ClaimsMapper.MapUserClaims(httpContext);

            if (userClaims?.AppMetaDataClaims?.Role != UserRoles.HomeAdmin)
            {
                return TypedResults.Problem(
                    detail: "User does not have permission to process estimates.",
                    statusCode: StatusCodes.Status403Forbidden
                );
            }

            try
            {
                var subscriptionItemId = GetSubscriptionItemId(
                    subscriptionService,
                    request.SubscriptionId,
                    request.CurrentPriceId
                );

                if (string.IsNullOrWhiteSpace(subscriptionItemId))
                {
                    return TypedResults.Problem(
                        detail: "Failed to retrieve subscription item ID.",
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }

                var invoice = await upcomingInvoiceService.UpcomingAsync(
                    new UpcomingInvoiceOptions
                    {
                        AutomaticTax = new InvoiceAutomaticTaxOptions { Enabled = true },
                        Customer = request.CustomerId,
                        Subscription = request.SubscriptionId,

                        SubscriptionItems = new List<InvoiceSubscriptionItemOptions>
                        {
                            new InvoiceSubscriptionItemOptions
                            {
                                Id = subscriptionItemId,
                                Price = request.NewPriceId,
                                Quantity = request.Quantity,
                            },
                        },

                        SubscriptionProrationBehavior = "always_invoice",
                        SubscriptionBillingCycleAnchor = SubscriptionBillingCycleAnchor.Unchanged,
                    }
                );

                var lines = invoice
                    .Lines.Data.Select(line => new InvoiceItemDto
                    {
                        Description = line.Description,
                        Amount = line.Amount,
                        Quantity = line.Quantity,
                        Interval = line.Price.Recurring.Interval,
                        Tiers =
                            line.Price?.Tiers?.Select(tier => new TieredPricingDto
                                {
                                    UpTo = tier.UpTo,
                                    UnitAmount = tier.UnitAmount,
                                })
                                .ToList() ?? new List<TieredPricingDto>(),
                    })
                    .ToList();
                var lastLineItem = invoice.Lines.Data.LastOrDefault();
                if (lastLineItem == null)
                {
                    return TypedResults.Problem(
                        detail: "Failed to extract the latest line item from the invoice.",
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }

                return TypedResults.Ok(
                    new EstimateResponse
                    {
                        Subtotal = invoice.Subtotal,
                        Taxes = invoice.Tax,
                        TotalDueToday = invoice.AmountDue,
                        TotalDueNextBilling = invoice.Total,
                        NextPaymentDate = invoice.NextPaymentAttempt,
                        PeriodStart = lastLineItem.Period.Start,
                        PeriodEnd = lastLineItem.Period.End,
                        Items = lines,
                        DueDate = invoice.DueDate ?? invoice.NextPaymentAttempt ?? DateTime.Now,
                    }
                );
            }
            catch (StripeException ex)
            {
                return TypedResults.Problem(
                    detail: $"Stripe API error: {ex.Message}",
                    statusCode: (int)ex.HttpStatusCode
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex}");
                return TypedResults.Problem(
                    detail: "An unexpected error occurred while estimating prorates.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }

        private static async Task<
            Results<Ok<PortalSessionResponse>, ProblemHttpResult>
        > CreatePortalSession(
            [FromBody] PortalSessionRequest request,
            [FromServices] Stripe.BillingPortal.SessionService portalSessionService,
            HttpContext httpContext
        )
        {
            var userClaims = ClaimsMapper.MapUserClaims(httpContext);
            var customerId = userClaims?.AppMetaDataClaims?.SubscriptionCustomerId;
            var subscriptionId = userClaims?.AppMetaDataClaims?.SubscriptionId;

            if (string.IsNullOrWhiteSpace(customerId) || string.IsNullOrWhiteSpace(subscriptionId))
            {
                return TypedResults.Problem(
                    detail: "Stripe Customer ID or Subscription ID are not found for user.",
                    statusCode: StatusCodes.Status400BadRequest
                );
            }

            var returnUrl = $"{DOMAIN_BASE_URL}{request.ReturnPath}";

            try
            {
                var options = new Stripe.BillingPortal.SessionCreateOptions
                {
                    Customer = customerId,
                    ReturnUrl = returnUrl,
                    FlowData = new Stripe.BillingPortal.SessionFlowDataOptions
                    {
                        Type =
                            request.Action == "update"
                                ? "subscription_update"
                                : "subscription_cancel",
                        SubscriptionCancel =
                            request.Action == "cancel"
                                ? new Stripe.BillingPortal.SessionFlowDataSubscriptionCancelOptions
                                {
                                    Subscription = subscriptionId,
                                }
                                : null,
                        SubscriptionUpdate =
                            request.Action == "update"
                                ? new Stripe.BillingPortal.SessionFlowDataSubscriptionUpdateOptions
                                {
                                    Subscription = subscriptionId,
                                }
                                : null,
                    },
                };

                var session = await portalSessionService.CreateAsync(options);

                return TypedResults.Ok(new PortalSessionResponse(session.Url));
            }
            catch (StripeException ex)
            {
                return TypedResults.Problem(
                    detail: $"Stripe Error: {ex.StripeError?.Message ?? ex.Message}",
                    statusCode: (int)ex.HttpStatusCode
                );
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"Internal Server Error creating portal session: {ex.Message}"
                );
                return TypedResults.Problem(
                    detail: "Internal server error.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }

        public static async Task<
            Results<Ok<EstimateResponse>, ProblemHttpResult>
        > GetInvoicePreview(
            [FromBody] InvoicePreviewRequest request,
            [FromServices] SubscriptionService subscriptionService,
            [FromServices] InvoiceService upcomingInvoiceService,
            [FromServices] SubscriptionDBService subscriptionDBService,
            HttpContext httpContext
        )
        {
            var userClaims = ClaimsMapper.MapUserClaims(httpContext);

            if (userClaims?.AppMetaDataClaims?.Role != UserRoles.HomeAdmin)
            {
                return TypedResults.Problem(
                    detail: "User does not have permission to process estimates.",
                    statusCode: StatusCodes.Status403Forbidden
                );
            }

            try
            {
                var address = HttpContextHelper.GetClientIp(httpContext);

                var options = new InvoiceCreatePreviewOptions
                {
                    Currency = request.Currency,
                    SubscriptionDetails = new InvoiceSubscriptionDetailsOptions
                    {
                        Items =
                        [
                            new InvoiceSubscriptionDetailsItemOptions
                            {
                                Price = request.PriceId,
                                Quantity = request.Quantity,
                            },
                        ],
                        BillingCycleAnchor = request.SkipBillingCycleAnchor
                            ? null
                            : subscriptionDBService.GetFirstDayOfTheNextMonthUTCSeconds(),
                    },

                    AutomaticTax = new InvoiceAutomaticTaxOptions
                    {
                        Enabled = address != null ? true : false,
                    },

                    CustomerDetails = new InvoiceCustomerDetailsOptions
                    {
                        Tax = new InvoiceCustomerDetailsTaxOptions { IpAddress = address },
                    },
                };

                var invoice = await upcomingInvoiceService.CreatePreviewAsync(options);

                var lines = invoice
                    .Lines.Data.Select(line => new InvoiceItemDto
                    {
                        Description = line.Description,
                        Amount = line.Amount,
                        Quantity = line.Quantity,
                        Interval = line.Price.Recurring.Interval,
                        Tiers =
                            line.Price?.Tiers?.Select(tier => new TieredPricingDto
                                {
                                    UpTo = tier.UpTo,
                                    UnitAmount = tier.UnitAmount,
                                })
                                .ToList() ?? new List<TieredPricingDto>(),
                    })
                    .ToList();

                var lastLineItem = invoice.Lines.Data.LastOrDefault();

                if (lastLineItem == null)
                {
                    return TypedResults.Problem(
                        detail: "Failed to extract the latest line item from the invoice.",
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }

                return TypedResults.Ok(
                    new EstimateResponse
                    {
                        Subtotal = invoice.Subtotal,
                        Taxes = invoice.Tax,
                        TotalDueToday = invoice.AmountDue,
                        TotalDueNextBilling = invoice.Total,
                        NextPaymentDate = invoice.NextPaymentAttempt,
                        PeriodStart = lastLineItem.Period.Start,
                        PeriodEnd = lastLineItem.Period.End,
                        DueDate = invoice.DueDate ?? invoice.NextPaymentAttempt ?? DateTime.Now,
                        Items = lines,
                    }
                );
            }
            catch (StripeException ex)
            {
                return TypedResults.Problem(
                    detail: $"Stripe API error: {ex.Message}",
                    statusCode: (int)ex.HttpStatusCode
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex}");
                return TypedResults.Problem(
                    detail: "An unexpected error occurred while estimating prorates.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }

        private static string? GetSubscriptionItemId(
            SubscriptionService service,
            string subscriptionId,
            string currentPriceId
        )
        {
            var subscription = service.Get(subscriptionId);

            if (subscription?.Items?.Data == null || subscription.Items.Data.Count == 0)
            {
                return null;
            }

            var matchingItem = subscription.Items.Data.FirstOrDefault(item =>
                item.Price.Id == currentPriceId && item.Quantity > 0
            );

            if (matchingItem != null)
            {
                return matchingItem.Id;
            }

            return null;
        }
    }
}
