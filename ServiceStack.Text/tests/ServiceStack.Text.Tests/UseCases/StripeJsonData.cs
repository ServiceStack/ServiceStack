namespace ServiceStack.Text.Tests.UseCases
{
    public static class StripeJsonData
    {
        public const string Invoice = @"{
  ""date"": 1384730085,
  ""id"": ""in_2xZuT4g9lqF2KN"",
  ""period_start"": 1384730085,
  ""period_end"": 1384730085,
  ""lines"": {
    ""data"": [
      {
        ""id"": ""sub_2xmgz6OwkWhW4r"",
        ""object"": ""line_item"",
        ""type"": ""subscription"",
        ""livemode"": true,
        ""amount"": 79900,
        ""currency"": ""usd"",
        ""proration"": false,
        ""period"": {
          ""start"": 1416313625,
          ""end"": 1447849625
        },
        ""quantity"": 1,
        ""plan"": {
          ""interval"": ""year"",
          ""name"": ""1x Business Developer (All ServiceStack)"",
          ""amount"": 79900,
          ""currency"": ""usd"",
          ""id"": ""BUS-SUB"",
          ""object"": ""plan"",
          ""livemode"": false,
          ""interval_count"": 1,
          ""trial_period_days"": null,
          ""metadata"": {
          }
        },
        ""description"": null,
        ""metadata"": null
      }
    ],
    ""count"": 1,
    ""object"": ""list"",
    ""url"": ""/v1/invoices/in_2xZuT4g9lqF2KN/lines""
  },
  ""subtotal"": 191800,
  ""total"": 191800,
  ""customer"": ""cus_2xZtV7PEI9zw2k"",
  ""object"": ""invoice"",
  ""attempted"": true,
  ""closed"": true,
  ""paid"": true,
  ""livemode"": false,
  ""attempt_count"": 0,
  ""amount_due"": 191800,
  ""currency"": ""usd"",
  ""starting_balance"": 0,
  ""ending_balance"": 0,
  ""next_payment_attempt"": null,
  ""charge"": ""ch_2xZuWjWDcpa49u"",
  ""discount"": null,
  ""application_fee"": null
}";

        public const string Customer = @"
{
  ""object"": ""customer"",
  ""created"": 1384730053,
  ""id"": ""cus_2xZtV7PEI9zw2k"",
  ""livemode"": false,
  ""description"": ""Demis Bellot"",
  ""email"": ""demis.bellot@gmail.com"",
  ""delinquent"": false,
  ""metadata"": {
  },
  ""subscription"": {
    ""id"": ""sub_2xZuDcfSjCZo7N"",
    ""plan"": {
      ""interval"": ""year"",
      ""name"": ""4 Cores (All ServiceStack)"",
      ""amount"": 191800,
      ""currency"": ""usd"",
      ""id"": ""CORE-04-SUB"",
      ""object"": ""plan"",
      ""livemode"": false,
      ""interval_count"": 1,
      ""trial_period_days"": null,
      ""metadata"": {
      }
    },
    ""object"": ""subscription"",
    ""start"": 1384730085,
    ""status"": ""active"",
    ""customer"": ""cus_2xZtV7PEI9zw2k"",
    ""cancel_at_period_end"": false,
    ""current_period_start"": 1384730085,
    ""current_period_end"": 1416266085,
    ""ended_at"": null,
    ""trial_start"": null,
    ""trial_end"": null,
    ""canceled_at"": null,
    ""quantity"": 1,
    ""application_fee_percent"": null
  },
  ""discount"": null,
  ""account_balance"": 0,
  ""cards"": {
    ""object"": ""list"",
    ""count"": 1,
    ""url"": ""/v1/customers/cus_2xZtV7PEI9zw2k/cards"",
    ""data"": [
      {
        ""id"": ""card_2xZuMH4Ef4dYta"",
        ""object"": ""card"",
        ""last4"": ""4242"",
        ""type"": ""Visa"",
        ""exp_month"": 1,
        ""exp_year"": 2014,
        ""fingerprint"": ""P7ROm12oDQlM4Iw2"",
        ""customer"": ""cus_2xZtV7PEI9zw2k"",
        ""country"": ""US"",
        ""name"": null,
        ""address_line1"": null,
        ""address_line2"": null,
        ""address_city"": null,
        ""address_state"": null,
        ""address_zip"": null,
        ""address_country"": null,
        ""cvc_check"": ""pass"",
        ""address_line1_check"": null,
        ""address_zip_check"": null
      }
    ]
  },
  ""default_card"": ""card_2xZuMH4Ef4dYta""
}";

        public const string Coupon = @"
{
  ""id"": ""MULTI10"",
  ""percent_off"": 20,
  ""amount_off"": null,
  ""currency"": ""usd"",
  ""object"": ""coupon"",
  ""livemode"": false,
  ""duration"": ""forever"",
  ""redeem_by"": null,
  ""max_redemptions"": null,
  ""times_redeemed"": 5,
  ""duration_in_months"": null
}";

        public const string Discount = @"
{
  ""coupon"": {
    ""id"": ""MULTI10"",
    ""percent_off"": 20,
    ""amount_off"": null,
    ""currency"": ""usd"",
    ""object"": ""coupon"",
    ""livemode"": false,
    ""duration"": ""forever"",
    ""redeem_by"": null,
    ""max_redemptions"": null,
    ""times_redeemed"": 5,
    ""duration_in_months"": null
  },
  ""start"": 1384560547,
  ""object"": ""discount"",
  ""customer"": ""cus_2xZtV7PEI9zw2k"",
  ""end"": null
}";

        public const string Card = @"
{
  ""id"": ""card_2xZuMH4Ef4dYta"",
  ""object"": ""card"",
  ""last4"": ""4242"",
  ""type"": ""Visa"",
  ""exp_month"": 1,
  ""exp_year"": 2014,
  ""fingerprint"": ""P7ROm12oDQlM4Iw2"",
  ""customer"": null,
  ""country"": ""US"",
  ""name"": null,
  ""address_line1"": null,
  ""address_line2"": null,
  ""address_city"": null,
  ""address_state"": null,
  ""address_zip"": null,
  ""address_country"": null,
  ""cvc_check"": ""pass"",
  ""address_line1_check"": null,
  ""address_zip_check"": null
}";

        public const string Charge = @"
{
  ""id"": ""ch_2xZuWjWDcpa49u"",
  ""object"": ""charge"",
  ""created"": 1384730085,
  ""livemode"": false,
  ""paid"": true,
  ""amount"": 191800,
  ""currency"": ""usd"",
  ""refunded"": false,
  ""card"": {
    ""id"": ""card_2xZuMH4Ef4dYta"",
    ""object"": ""card"",
    ""last4"": ""4242"",
    ""type"": ""Visa"",
    ""exp_month"": 1,
    ""exp_year"": 2014,
    ""fingerprint"": ""P7ROm12oDQlM4Iw2"",
    ""customer"": null,
    ""country"": ""US"",
    ""name"": null,
    ""address_line1"": null,
    ""address_line2"": null,
    ""address_city"": null,
    ""address_state"": null,
    ""address_zip"": null,
    ""address_country"": null,
    ""cvc_check"": ""pass"",
    ""address_line1_check"": null,
    ""address_zip_check"": null
  },
  ""captured"": true,
  ""refunds"": [

  ],
  ""balance_transaction"": ""txn_2xX439hAPU0Rmb"",
  ""failure_message"": null,
  ""failure_code"": null,
  ""amount_refunded"": 0,
  ""customer"": ""cus_2xZtV7PEI9zw2k"",
  ""invoice"": ""in_2xZuT4g9lqF2KN"",
  ""description"": null,
  ""dispute"": null,
  ""metadata"": {
  }
}";

    }


}