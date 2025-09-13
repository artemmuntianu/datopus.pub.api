using datopus.api.Core.Enums;
using datopus.api.Core.Services;
using datopus.Application.Services;
using datopus.Application.Services.Subscriptions;
using datopus.Core.Entities.DbEntities;
using datopus.Core.Enums.Constants;
using Microsoft.AspNetCore.Http.HttpResults;
using Newtonsoft.Json;
using Stripe;
using Supabase.Gotrue;
using Supabase.Gotrue.Exceptions;

namespace datopus.Api.Endpoints
{
    public static class SignupEndpoints
    {
        public static void Register(WebApplication app)
        {
            var endpoints = app.MapGroup("/signup");
            endpoints.MapGet("/confirm", Confirm);
        }

        public static async Task<RedirectHttpResult> Confirm(
            string token_hash,
            string redirect_to,
            AuthService auth,
            DbService db,
            PriceService priceService,
            SubscriptionDBService subscriptionPriceService,
            BQDashboardService dashboardService
        )
        {
            var subscriptionName = "optimize_mo";

            try
            {
                var session = await auth.VerifyTokenHash(token_hash);
                var user = session!.User!;

                var newOrg = await db.AddOrganization(
                    new Org
                    {
                        Name = user.UserMetadata["orgName"].ToString()!,
                        Type = user.UserMetadata["orgType"].ToString()!,
                        Subscription = subscriptionName,
                    }
                );

                await dashboardService.AddDefaultDashboard(newOrg.Id, user.Id!);

                await db.AddOnboardingProgress(
                    new OnboardingProgress
                    {
                        OrgId = newOrg.Id,
                        SignupCompletedAt = DateTime.UtcNow,
                    }
                );

                var priceList = await priceService.ListAsync(
                    new PriceListOptions { LookupKeys = new List<string>([newOrg.Subscription]) }
                );
                var price = priceList.FirstOrDefault();

                var landingPageUser = await db.GetLandingPageUser(user.Email!);
                if (landingPageUser?.StartupRequested == null)
                    await SignupForTrial(newOrg, user, price, db, subscriptionPriceService);
                else
                    await SignupForStartupProgram(
                        newOrg,
                        user,
                        price,
                        db,
                        subscriptionPriceService
                    );
            }
            catch (GotrueException e)
            {
                Console.WriteLine(e.Message);
                redirect_to +=
                    $"?err={JsonConvert.DeserializeAnonymousType(e.Message, new { msg = "" })!.msg}";
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                redirect_to += $"?err={e.Message}";
            }

            return TypedResults.Redirect(redirect_to);
        }

        private static async Task SignupForTrial(
            Org org,
            User user,
            Price? price,
            DbService db,
            SubscriptionDBService subscriptionPriceService
        )
        {
            var subscriptionStatus = "trial";
            var timeNow = DateTime.UtcNow;
            var timeNowPlus30Days = timeNow.AddDays(30);

            await db.UpdateUserMetadata(
                user.Id!,
                appMetadata: new Dictionary<string, object>
                {
                    { "role", UserRoles.HomeAdmin },
                    { "orgId", org.Id },
                    { "orgType", org.Type },
                    { "orgSubscription", org.Subscription },
                    { "subscriptionStatus", subscriptionStatus },
                }
            );

            await subscriptionPriceService.UpsertSubscriptionAsync(
                new Core.Entities.DbEntities.Subscription
                {
                    OrgId = org.Id,
                    PriceId = price?.Id,
                    ProductId = price?.ProductId,
                    Quantity = 300000,
                    CurrentPeriodStart = timeNow,
                    CurrentPeriodEnd = timeNowPlus30Days,
                    TrialStarted = timeNow,
                    TrialEnded = timeNowPlus30Days,
                    StartDate = timeNow,
                    EndDate = timeNowPlus30Days,
                    Status = subscriptionStatus,
                    UpdatedAt = timeNow,
                    LookupKey = org.Subscription,
                }
            );

            try
            {
                await EmailService.SendMesssage(
                    user.Email!,
                    user.UserMetadata["full_name"].ToString()!,
                    EmailNotificationType.WelcomeEmailFreeTrial
                );
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static async Task SignupForStartupProgram(
            Org org,
            User user,
            Price? price,
            DbService db,
            SubscriptionDBService subscriptionPriceService
        )
        {
            var subscriptionStatus = "startup";
            var timeNow = DateTime.UtcNow;

            await db.UpdateUserMetadata(
                user.Id!,
                appMetadata: new Dictionary<string, object>
                {
                    { "role", UserRoles.HomeAdmin },
                    { "orgId", org.Id },
                    { "orgType", org.Type },
                    { "orgSubscription", org.Subscription },
                    { "subscriptionStatus", subscriptionStatus },
                }
            );

            await subscriptionPriceService.UpsertSubscriptionAsync(
                new Core.Entities.DbEntities.Subscription
                {
                    OrgId = org.Id,
                    PriceId = price?.Id,
                    ProductId = price?.ProductId,
                    Quantity = 1000,
                    StartDate = timeNow,
                    Status = subscriptionStatus,
                    UpdatedAt = timeNow,
                    LookupKey = org.Subscription,
                }
            );

            try
            {
                await EmailService.SendMesssage(
                    user.Email!,
                    user.UserMetadata["full_name"].ToString()!,
                    EmailNotificationType.WelcomeEmailStartupProgram
                );
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
