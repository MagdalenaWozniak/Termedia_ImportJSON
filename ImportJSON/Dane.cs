using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImportJSON
{
    public partial class Dane
    {
        [JsonProperty("success")]
        public string Status { get; set; }

        [JsonProperty("code")]
        public string Kod { get; set; }

        [JsonProperty("data")]
        public Faktura[] Faktury { get; set; }
    }


    public partial class Faktura
    {
        [JsonProperty("header")]
        public Nag Nag { get; set; }

        [JsonProperty("contractor")]
        public Knt Knt { get; set; }

        [JsonProperty("items")]
        public Elem[] Elemy { get; set; }
    }


    public partial class Nag
    {
        [JsonProperty("invoice_id")]
        public string Id { get; set; }

        [JsonProperty("invoice_type")]
        public string Typ { get; set; }

        [JsonProperty("currency")]
        public string Waluta { get; set; }

        [JsonProperty("is_paid")]
        public string Zaplacona { get; set; }

        [JsonProperty("payment_method")]
        public string Metoda_plat { get; set; }

        [JsonProperty("payment_transaction_id")]
        public string Id_plat { get; set; }

        [JsonProperty("date_insert")]
        public string Data_wys { get; set; }

        [JsonProperty("date_sale")]
        public string Data_sprzedazy { get; set; }

        [JsonProperty("date_due")]
        public string Termin { get; set; }

        [JsonProperty("proforma_number")]
        public string Nr_proformy { get; set; }

        [JsonProperty("description")]
        public string Opis { get; set; }
    }


    public partial class Knt
    {
        [JsonProperty("contractor_user_id")]
        public string Id { get; set; }

        [JsonProperty("contractor_tax_id")]
        public string Nip { get; set; }

        [JsonProperty("contractor_name")]
        public string Nazwisko { get; set; }

        [JsonProperty("contractor_postcode")]
        public string Kod_pocztowy { get; set; }

        [JsonProperty("contractor_city")]
        public string Miasto { get; set; }

        [JsonProperty("contractor_street")]
        public string Ulica { get; set; }

        [JsonProperty("contractor_email")]
        public string Email { get; set; }

        [JsonProperty("contractor_country_id")]
        public string Kraj_Id { get; set; }

    }


    public partial class Elem
    {
        [JsonProperty("comarch_gid")]
        public string Gid_numer { get; set; }

        [JsonProperty("quantity")]
        public string Ilosc { get; set; }

        [JsonProperty("tax")]
        public string Podatek { get; set; }

        [JsonProperty("price")]
        public string Cena { get; set; }

        [JsonProperty("price_discounted")]
        public string Cena_rabt { get; set; }


    }

    public partial class InfoZwrotne
    {
        public int invoiceId { get; set; }
        public int status { get; set; }
        public string invoiceNumber { get; set; }
    }

    public partial class InfoZwrotneBledy
    {
        public int invoiceId { get; set; }
        public int status { get; set; }
        public string msg { get; set; }
    }

    public partial class Odp
    {
        [JsonProperty("success")]
        public string Status { get; set; }

        [JsonProperty("code")]
        public string Kod { get; set; }

    }
    public partial class Odp
    {
        public static Odp FromJson(string json) => JsonConvert.DeserializeObject<Odp>(json);
    }



    public partial class Dane
    {
        public static Dane FromJson(string json) => JsonConvert.DeserializeObject<Dane>(json);
    }
}
