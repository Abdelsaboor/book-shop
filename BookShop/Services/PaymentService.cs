using BookShop.Models;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using System.Collections.Generic;

public class PaymentService
{
    private readonly StripeSettings _stripeSettings;

    public PaymentService(IOptions<StripeSettings> stripeSettings)
    {
        _stripeSettings = stripeSettings.Value;
        StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
    }

    public Session CreateCheckoutSession(List<SessionLineItemOptions> items, string successUrl, string cancelUrl)
    {
        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = items,
            Mode = "payment",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
        };

        var service = new SessionService();
        return service.Create(options);
    }
}
