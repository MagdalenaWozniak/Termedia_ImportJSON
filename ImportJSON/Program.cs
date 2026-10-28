using cdn_api;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ImportJSON
{
    class Program
    {
        private static  int XLWersjaAPI = 20232;
        private static int XLTrybWsadowy = 1;
        private static int XLWinieta = -1;
        private static int XLUtworzWlasnaSesje = 1;
        private static string XLSQLServer = "192.168.4.24";
        private static string XLSQLDatabase = "Termedia"; //"Termedia TEST";//"ERPXL_PoloMarket"
        private static string XLUser = "ADMIN"; //
        private static string XLPass = "NE!w0rk12";//"Dupa2003!";
        private static string connectionString = @"Data Source=192.168.4.24;Initial Catalog=ERPXL_Termedia; User ID=onesupport;Password=a956!2A@";
        static async Task Main()
        {
            try
            {
                TextWriter tw2 = new StreamWriter(@"C:\OneSupport\ImportJSON\log.txt", append: true);
                tw2.WriteLine();
                tw2.WriteLine();
                tw2.WriteLine();
                tw2.WriteLine();
                tw2.WriteLine("Hello " + DateTime.Now.ToString("f"));
                //tw2.WriteLine();
                tw2.WriteLine();

                Dane dane = new Dane();
                 dane=   await Http_JSON_Request(true, tw2);
                //Console.WriteLine("Code: " + dane.Kod);
                int sesja = 0;


                if (dane.Kod == "200")
                {

                    logowanie(ref sesja, tw2);

                    List<InfoZwrotne> info = new List<InfoZwrotne>();
                    List<InfoZwrotneBledy> infoBledy = new List<InfoZwrotneBledy>();

                    for (int i_dok=0; i_dok<dane.Faktury.Length; i_dok++)
                    {
                        string numerDok = "";
                        string blad = "";
                        tw2.WriteLine("Dodawanie faktury "+dane.Faktury[i_dok].Nag.Id);
                        string kntKod = CzyIstniejeKnt(dane.Faktury[i_dok].Knt);

                        if(kntKod=="")
                        {
                            tw2.WriteLine("Dodawanie kontrahenta " + dane.Faktury[i_dok].Knt.Nazwisko);
                            kntKod = DodajKnt(dane.Faktury[i_dok].Knt, tw2, sesja, ref blad);
                            tw2.WriteLine("Akronim dodanego kontrahenta: " + kntKod);
                        }

                        if(kntKod!="")
                        {
                            numerDok = DodajDok(dane.Faktury[i_dok], tw2, sesja, kntKod, ref blad);
                            tw2.WriteLine("Numer dodanego dokumentu: " + numerDok);
                        }

                        if(numerDok!="")
                        {
                            InfoZwrotne dok = new InfoZwrotne() ;
                            dok.invoiceId = Convert.ToInt32(dane.Faktury[i_dok].Nag.Id);
                            dok.status = 1;
                            dok.invoiceNumber = numerDok;
                            info.Add(dok);
                        }
                        else
                        {
                            InfoZwrotneBledy dok = new InfoZwrotneBledy();
                            dok.invoiceId= Convert.ToInt32(dane.Faktury[i_dok].Nag.Id);
                            dok.status = 0;
                            dok.msg = blad;
                            infoBledy.Add(dok);
                        }

                        tw2.WriteLine("");

                    }

                    tw2.WriteLine("Info zwrotne:");
                    foreach (InfoZwrotne dok in info)
                    {
                        tw2.WriteLine("invoiceId: " + dok.invoiceId + "\tstatus: " + dok.status + "\tinvoiceNumber: " + dok.invoiceNumber);
                    }
                    foreach (InfoZwrotneBledy dok in infoBledy)
                    {
                        tw2.WriteLine("invoiceId: " + dok.invoiceId + "\tstatus: " + dok.status + "\tmsg: " + dok.msg);
                    }

                    wylogowanie(sesja);

                    await Http_JSON_Response(true, tw2, info);
                    await Http_JSON_Response(true, tw2, infoBledy);
                }

                

                tw2.Close();

                //Console.ReadKey();
                
            }
            catch(Exception e)
            {
                Console.WriteLine("Błąd: " + e.Message);
                Console.ReadKey();
            }
        }


        static public async Task<Dane> Http_JSON_Request( bool SaveToFile, TextWriter tw2)
        {
            try
            {
                

                string json = "";
                Dane dane = new Dane();
                ServicePointManager.ServerCertificateValidationCallback +=
    (sender, cert, chain, sslPolicyErrors) => { return true; };

                System.Net.ServicePointManager.SecurityProtocol =
            SecurityProtocolType.Tls12 |
            SecurityProtocolType.Tls11 |
            SecurityProtocolType.Tls;



                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri("https://www.termedia.pl");
                    client.DefaultRequestHeaders.Clear();
                    //client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("pl-PL"));

                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    client.DefaultRequestHeaders.Add("apikey", "f14dd151-b503-41de-a7a0-879d7aa12d44");

                    var response = await client.GetAsync("/api/invoices");
                
                    // response.Content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json");
                    HttpContent responseContent = response.Content;
                    
                    //var baselinker_response = new Baselinker_response();
                    using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync()))
                    {

                        json = await reader.ReadToEndAsync();
                        dane = Dane.FromJson(json);
                        // baselinker_response = Baselinker_response.FromJson(json);
                        if (SaveToFile == true)
                        {
                            TextWriter tw = new StreamWriter(@"C:\OneSupport\ImportJSON\Input.txt");
                            tw.WriteLine(json);
                            tw.Close();
                        }


                    }
                    client.Dispose();



                    /*if (response.IsSuccessStatusCode)
                    {
                        var result = response.Content.ReadAsStringAsync().Result;
                        return Request.CreateResponse(HttpStatusCode.OK, result);
                    }

                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "fail");*/
                }
                return dane;
            }
            catch (Exception e)
            {
                Console.WriteLine("Błąd wywołania funkcji get: " + e.Message);
                tw2.WriteLine("Błąd wywołania funkcji get: " + e.Message);
                return new Dane();
            }
        }


        static public async Task<Odp> Http_JSON_Response(bool SaveToFile, TextWriter tw2, List<InfoZwrotne> data)
        {
            try
            {


                string json = "";
                Odp dane = new Odp();
                ServicePointManager.ServerCertificateValidationCallback +=
    (sender, cert, chain, sslPolicyErrors) => { return true; };

                System.Net.ServicePointManager.SecurityProtocol =
            SecurityProtocolType.Tls12 |
            SecurityProtocolType.Tls11 |
            SecurityProtocolType.Tls;



                using (var client = new HttpClient())
                {
                    //client.BaseAddress = new Uri("https://www.termedia.pl");
                    client.DefaultRequestHeaders.Clear();
                    //client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("pl-PL"));

                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    client.DefaultRequestHeaders.Add("apikey", "f14dd151-b503-41de-a7a0-879d7aa12d44");
                    //StringContent queryString = new StringContent(data)
                    //response.Content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json");
                    var jsonFile = JsonConvert.SerializeObject(data);
                    var content = new StringContent(jsonFile, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAsync(
                "https://www.termedia.pl/api/invoices",
                content);
                    //HttpContent responseContent = response.Content;
                    //var response = await client.PostAsync();//("/api/invoices", );

                    // 
                    HttpContent responseContent = response.Content;

                    //var baselinker_response = new Baselinker_response();
                    using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync()))
                    {

                        json = await reader.ReadToEndAsync();
                        dane = Odp.FromJson(json);
                        // baselinker_response = Baselinker_response.FromJson(json);
                        if (SaveToFile == true)
                        {
                            //TextWriter tw = new StreamWriter(@"C:\OneSupport\ImportJSON\Input.txt");
                            tw2.WriteLine(json);
                            //tw.Close();
                        }


                    }
                    client.Dispose();



                    /*if (response.IsSuccessStatusCode)
                    {
                        var result = response.Content.ReadAsStringAsync().Result;
                        return Request.CreateResponse(HttpStatusCode.OK, result);
                    }

                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "fail");*/
                }
                return dane;
            }
            catch (Exception e)
            {
                Console.WriteLine("Błąd wywołania funkcji POST: " + e.Message);
                tw2.WriteLine("Błąd wywołania funkcji POST: " + e.Message);
                return new Odp();
            }
        }


        static public async Task<Odp> Http_JSON_Response(bool SaveToFile, TextWriter tw2, List<InfoZwrotneBledy> data)
        {
            try
            {


                string json = "";
                Odp dane = new Odp();
                ServicePointManager.ServerCertificateValidationCallback +=
    (sender, cert, chain, sslPolicyErrors) => { return true; };

                System.Net.ServicePointManager.SecurityProtocol =
            SecurityProtocolType.Tls12 |
            SecurityProtocolType.Tls11 |
            SecurityProtocolType.Tls;



                using (var client = new HttpClient())
                {
                    //client.BaseAddress = new Uri("https://www.termedia.pl");
                    client.DefaultRequestHeaders.Clear();
                    //client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("pl-PL"));

                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    client.DefaultRequestHeaders.Add("apikey", "f14dd151-b503-41de-a7a0-879d7aa12d44");
                    //StringContent queryString = new StringContent(data)
                    //response.Content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json");
                    var jsonFile = JsonConvert.SerializeObject(data);
                    var content = new StringContent(jsonFile, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAsync(
                "https://www.termedia.pl/api/invoices",
                content);
                    //HttpContent responseContent = response.Content;
                    //var response = await client.PostAsync();//("/api/invoices", );

                    // 
                    HttpContent responseContent = response.Content;

                    //var baselinker_response = new Baselinker_response();
                    using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync()))
                    {

                        json = await reader.ReadToEndAsync();
                        dane = Odp.FromJson(json);
                        // baselinker_response = Baselinker_response.FromJson(json);
                        if (SaveToFile == true)
                        {
                            //TextWriter tw = new StreamWriter(@"C:\OneSupport\ImportJSON\Input.txt");
                            tw2.WriteLine(json);
                            //tw.Close();
                        }


                    }
                    client.Dispose();


                }
                return dane;
            }
            catch (Exception e)
            {
                Console.WriteLine("Błąd wywołania funkcji POST Błędy: " + e.Message);
                tw2.WriteLine("Błąd wywołania funkcji POST Błędy: " + e.Message);
                return new Odp();
            }
        }

        static public void logowanie (ref int sesja, TextWriter tw2)
        {
            XLLoginInfo_20232 nowyUzytkownik = new XLLoginInfo_20232();
            nowyUzytkownik.Wersja = XLWersjaAPI;
            nowyUzytkownik.Winieta = XLWinieta;
            nowyUzytkownik.TrybWsadowy = XLTrybWsadowy;
            nowyUzytkownik.UtworzWlasnaSesje = XLUtworzWlasnaSesje;
            nowyUzytkownik.SerwerKlucza = XLSQLServer;
            nowyUzytkownik.Baza = XLSQLDatabase;
            nowyUzytkownik.OpeIdent = XLUser;
            nowyUzytkownik.OpeHaslo = XLPass;
            nowyUzytkownik.PortHTTPSerweraKlucza = 1;
            nowyUzytkownik.RodzajSerweraKlucza = 0;
            nowyUzytkownik.SerwerKlucza = "localhost::5000185870";
            nowyUzytkownik.ProgramID = "ONE-041 Import JSON";

            int blad = cdn_api.cdn_api.XLLogin(nowyUzytkownik, ref sesja);

            if (blad != 0)
            {
                tw2.WriteLine("Błąd logowania: " + blad);
            }

        }

        static public void wylogowanie(int sesja)
        {
            cdn_api.cdn_api.XLLogout(sesja);
        }

        static public string CzyIstniejeKnt(Knt knt)
        {
            string kntKod = "";
            if(knt.Nip!="")
            {
                string nipQuery = "SELECT TOP (1) Knt_Akronim FROM cdn.KntKarty WHERE Knt_Nip = '" + knt.Nip + "' or Knt_NipE = '" + knt.Nip + "'";
                kntKod = ConnectToSQL(connectionString, nipQuery);
            }


            if(kntKod=="")
            {
                string nazwaQuery = "SELECT TOP (1) Knt_Akronim FROM cdn.KntKarty WHERE Trim(Knt_Nazwa1 + Knt_Nazwa2 + Knt_Nazwa3) = '"+knt.Nazwisko+"' and Knt_KodP='"+knt.Kod_pocztowy+"'";
                kntKod = ConnectToSQL(connectionString, nazwaQuery);
            }

            return kntKod;
        }


        static public string DodajKnt(Knt knt, TextWriter tw2, int sesja, ref string  bladString)
        {
            try
            {
                string kraj = "";
                try
                {
                    if (knt.Kraj_Id != null)
                    {
                        kraj = krajKnt(knt.Kraj_Id);
                    }
                    else
                        kraj = "Polska";
                }
                catch(Exception e )
                {
                    tw2.WriteLine("Błąd przypisywania kraju" + e.Message);
                    kraj = "Polska";


                }


                string krajKodQuery = "SELECT KPC_Kod FROM cdn.KrajeCelne WHERE KPC_NazwaPL = '" + kraj + "' ";
                string krajKod = ConnectToSQL(connectionString, krajKodQuery);

                try
                {
                    if (knt.Id == null)
                    {
                        string gidNumerQuery = "SELECT MAX(KNT_GIDNumer) from cdn.KntKarty";
                        string gidNumer = ConnectToSQL(connectionString, gidNumerQuery);
                        knt.Id = Convert.ToString(Convert.ToInt32(gidNumer) + 1);

                    }
                }
                catch(Exception en)
                {
                    tw2.WriteLine("Błąd przypisywania knt.id " + en.Message);
                }
                

                string kntAkronim = knt.Id + "#" + (knt.Nazwisko.Length > (20 - ((knt.Id.Length) + 1)) ? knt.Nazwisko.Substring(0, 20 - ((knt.Id.Length) + 1)) : knt.Nazwisko);

                int nazwiskoCount = knt.Nazwisko.Count();

                XLKontrahentInfo_20232 kontrahent = new XLKontrahentInfo_20232();
                kontrahent.Wersja = XLWersjaAPI;
                kontrahent.Akronim = kntAkronim; //knt.Nazwisko.Length > 20 ? knt.Nazwisko.Substring(0, 20) : knt.Nazwisko;
                kontrahent.KodP = knt.Kod_pocztowy;
                kontrahent.Miasto = knt.Miasto;
                kontrahent.Nazwa1 = nazwiskoCount>50? knt.Nazwisko.Substring(0,50): knt.Nazwisko;
                kontrahent.Nazwa2 = nazwiskoCount > 50 ? (nazwiskoCount < 100 ? knt.Nazwisko.Substring(50, nazwiskoCount - 50) : knt.Nazwisko.Substring(50, 50)) : "";
                kontrahent.Nazwa3 = nazwiskoCount > 100 ? knt.Nazwisko.Substring(100, nazwiskoCount - 100) : "";
                kontrahent.Ulica = knt.Ulica;
                kontrahent.EMail = knt.Email;
                kontrahent.Kraj = krajKod;




                if (knt.Nip != "")
                {
                    kontrahent.NipPrefiks = krajKod;
                    kontrahent.NipE = knt.Nip;
                    kontrahent.Status = 1;
                    kontrahent.PlatnikVat = 1;
                }
                else
                {
                    kontrahent.Status = 2;
                }


                string czzyUEQuery = "if exists (SELECT * FROM cdn.KrajeCelne WHERE KPC_NazwaPL = '" + kraj + "' and KPC_GKrID = 1) SELECt 'TAK' else SELECT 'NIE'";
                string czyUE = ConnectToSQL(connectionString, czzyUEQuery);

                if (knt.Kraj_Id == "170")
                {
                    kontrahent.ExpoKraj = 1;
                }
                else if (czyUE.Trim() == "TAK")
                {
                    kontrahent.ExpoKraj = 2;
                }
                else
                {
                    kontrahent.ExpoKraj = 3;
                }


                //tutaj jeszcze cała zabawa z krajem;

                int kntID = 0;

                int blad = cdn_api.cdn_api.XLNowyKontrahent(sesja, ref kntID, kontrahent);
                if (blad != 0)
                {
                    tw2.WriteLine("Błąd dodawania kontrahenta " + knt.Nazwisko + ": " + blad);
                    bladString = "Błąd dodawania kontrahenta " + knt.Nazwisko + ": " + blad;
                    if (blad == 48)
                    {
                        bladString = "Błąd dodawania kontrahenta " + knt.Nazwisko + ": Kontrahent już istnieje, ale z innym numerem NIP";
                    }
                    return "";
                }



                XLZamkniecieKontrahentaInfo_20232 kntZam = new XLZamkniecieKontrahentaInfo_20232();
                kntZam.Wersja = XLWersjaAPI;
                blad = cdn_api.cdn_api.XLZamknijKontrahenta(kntID, kntZam);
                if (blad != 0)
                {
                    tw2.WriteLine("Błąd zamykania kontrahenta " + knt.Nazwisko + ": " + blad);
                    bladString = "Błąd zamykania kontrahenta " + knt.Nazwisko + ": " + blad;
                    return "";
                }

                return (knt.Id + "#" + (knt.Nazwisko.Length > (20 - ((knt.Id.Length) + 1)) ? knt.Nazwisko.Substring(0, 20 - ((knt.Id.Length) + 1)) : knt.Nazwisko));
            }
            catch (Exception e)
            {
                tw2.WriteLine("Błąd dodawania kontrahenta " + knt.Nazwisko + ": " + e.Message) ;
                bladString = "Błąd dodawania kontrahenta " + knt.Nazwisko;
                return "";
            }
        }



        static public string DodajDok(Faktura faktura, TextWriter tw2, int sesja, string kntKod, ref string bladString)
        {
            try
            {


                int dokId = 0;
                XLDokumentNagInfo_20232 dokNag = new XLDokumentNagInfo_20232();
                dokNag.Wersja = XLWersjaAPI;
                dokNag.Akronim = kntKod;
                dokNag.GIDFirma = 0;
                dokNag.GIDNumer = 0;
                dokNag.GIDTyp = 0;
                dokNag.GIDLp = 0;
                dokNag.Termin = Convert.ToInt32(ConnectToSQL(connectionString, "SELECT datediff(day, '18001228', '" + faktura.Nag.Termin + "')"));
                dokNag.DataSpr = Convert.ToInt32(ConnectToSQL(connectionString, "SELECT datediff(day, '18001228', '" + faktura.Nag.Data_sprzedazy + "')"));
                dokNag.FRSID = 6;
                dokNag.Waluta = faktura.Nag.Waluta;
                dokNag.Opis = faktura.Nag.Opis;
                if (faktura.Nag.Metoda_plat.Trim() == "CASH")
                {
                    dokNag.Forma = 10;
                }
                else if (faktura.Nag.Metoda_plat.Trim() == "BANK_CARD")
                {
                    dokNag.Forma = 50;
                }
                else dokNag.Forma = 24;

                //dokNag.Opis=faktura.Nag.


                string czyKonferencjaQuery = "if exists( SELECT * FROM cdn.TwrKarty WHERE cdn.ONE_TwrGrupaPelnaNazwa(Twr_GIDNumer) like '%KONFERENCJE%' and Twr_GIDNumer = " + faktura.Elemy[0].Gid_numer + ") SELECT 'TAK' " +
                                            " else SELECT 'NIE'";
                string czyKonf = ConnectToSQL(connectionString, czyKonferencjaQuery);

                string czyCzasopismQuery = "if exists( SELECT * FROM cdn.TwrKarty WHERE (cdn.ONE_TwrGrupaPelnaNazwa(Twr_GIDNumer) like '%KSIĄŻKI%' or cdn.ONE_TwrGrupaPelnaNazwa(Twr_GIDNumer) like '%E-BOOK%' "
                                            + " or cdn.ONE_TwrGrupaPelnaNazwa(Twr_GIDNumer) like '%CZASOPISMA%') and Twr_GIDNumer = 708) SELECT 'TAK' else SELECT 'NIE'";
                string czyCzasopism = ConnectToSQL(connectionString, czyCzasopismQuery);

                if (czyKonf.Trim() == "TAK" || faktura.Knt.Nip != "")
                {
                    //FS
                    dokNag.Typ = 2033;
                    if (czyCzasopism.Trim() == "TAK")
                        dokNag.Seria = "FSL";
                    else if (czyKonf.Trim() == "TAK")
                        dokNag.Seria = "FKF";
                }
                else if (faktura.Knt.Nip == "" && faktura.Knt.Kraj_Id == "170")
                {
                    //paragon
                    dokNag.Typ = 2034;
                    if (czyCzasopism.Trim() == "TAK")
                        dokNag.Seria = "PARL";
                }
                else if (faktura.Knt.Kraj_Id != "170" || faktura.Nag.Waluta != "PLN")
                {
                    //FSE
                    dokNag.Typ = 2037;
                    dokNag.Seria = "SZAW";
                }
                else
                {
                    tw2.WriteLine("Z założeń nie można określić typu dokumentu: " + faktura.Nag.Id);
                    bladString = "Z założeń nie można określić typu dokumentu: " + faktura.Nag.Id;
                    return "";
                }

                int blad = cdn_api.cdn_api.XLNowyDokument(sesja, ref dokId, dokNag);
                if (blad != 0)
                {
                    tw2.WriteLine("Błąd dodawania faktury " + faktura.Nag.Id + ": " + blad);
                    bladString = "Błąd dodawania faktury " + faktura.Nag.Id + ": " + blad;
                    return "";
                }


                int tryb = 1;

                for (int i = 0; i < faktura.Elemy.Count(); i++)
                {
                    blad = DodajElem(faktura.Elemy[i], tw2, sesja, dokId, ref bladString);
                    if (blad != 0)
                    {
                        tryb = -1;
                        break;
                    }
                }


                XLZamkniecieDokumentuInfo_20232 zamDok = new XLZamkniecieDokumentuInfo_20232();
                zamDok.Wersja = XLWersjaAPI;
                zamDok.Tryb = tryb;
                cdn_api.cdn_api.XLZamknijDokument(dokId, zamDok);
                if (tryb == -1)
                    return "";

                string numerDok = "";

                if (tryb == 1)
                {
                    XLAtrybutInfo_20232 atr = new XLAtrybutInfo_20232();
                    atr.Wersja = XLWersjaAPI;
                    atr.GIDFirma = dokNag.GIDFirma;
                    atr.GIDLp = dokNag.GIDLp;
                    atr.GIDNumer = dokNag.GIDNumer;
                    atr.GIDTyp = dokNag.GIDTyp;
                    atr.GIDSubLp = 0;
                    atr.Klasa = "id_platnosci";
                    atr.Wartosc = faktura.Nag.Id_plat;
                    blad = cdn_api.cdn_api.XLDodajAtrybut(sesja, atr);
                    if (blad != 0)
                    {
                        tw2.WriteLine("Błąd dodawania atrybutu id_platnosci " + faktura.Nag.Id + ": " + blad);

                    }

                    XLAtrybutInfo_20232 atr2 = new XLAtrybutInfo_20232();
                    atr2.Wersja = XLWersjaAPI;
                    atr2.GIDFirma = dokNag.GIDFirma;
                    atr2.GIDLp = dokNag.GIDLp;
                    atr2.GIDNumer = dokNag.GIDNumer;
                    atr2.GIDTyp = dokNag.GIDTyp;
                    atr2.GIDSubLp = 0;
                    atr2.Klasa = "ID_Termedia";
                    atr2.Wartosc = faktura.Nag.Id;
                    blad = cdn_api.cdn_api.XLDodajAtrybut(sesja, atr2);
                    if (blad != 0)
                    {
                        tw2.WriteLine("Błąd dodawania atrybutu ID_Termedia " + faktura.Nag.Id + ": " + blad);

                    }

                    XLAtrybutInfo_20232 atr3 = new XLAtrybutInfo_20232();
                    atr3.Wersja = XLWersjaAPI;
                    atr3.GIDFirma = dokNag.GIDFirma;
                    atr3.GIDLp = dokNag.GIDLp;
                    atr3.GIDNumer = dokNag.GIDNumer;
                    atr3.GIDTyp = dokNag.GIDTyp;
                    atr3.GIDSubLp = 0;
                    atr3.Klasa = "Nr Proformy";
                    atr3.Wartosc = faktura.Nag.Nr_proformy;
                    blad = cdn_api.cdn_api.XLDodajAtrybut(sesja, atr3);
                    if (blad != 0)
                    {
                        tw2.WriteLine("Błąd dodawania atrybutu Nr Proformy" + faktura.Nag.Nr_proformy + ": " + blad);

                    }

                    XLAtrybutInfo_20232 atr4 = new XLAtrybutInfo_20232();
                    atr4.Wersja = XLWersjaAPI;
                    atr4.GIDFirma = dokNag.GIDFirma;
                    atr4.GIDLp = dokNag.GIDLp;
                    atr4.GIDNumer = dokNag.GIDNumer;
                    atr4.GIDTyp = dokNag.GIDTyp;
                    atr4.GIDSubLp = 0;
                    atr4.Klasa = "Nr Faktury Prenumeraty";
                    atr4.Wartosc = faktura.Nag.Nr_proformy;
                    blad = cdn_api.cdn_api.XLDodajAtrybut(sesja, atr4);
                    if (blad != 0)
                    {
                        tw2.WriteLine("Błąd dodawania atrybutu Nr Faktury Prenumeraty" + faktura.Nag.Nr_proformy + ": " + blad);

                    }

                    string numerDokQuery = "SELECT cdn.NumerDokumentu(TrN_GIDTyp, TrN_SpiTyp, TrN_TrNTyp, TrN_TrNNumer, TrN_TrNRok, TrN_TrNSeria, TrN_TrNMiesiac) from cdn.TraNag WHERE TrN_GIDTyp = " + dokNag.GIDTyp
                                        + " and TrN_GIDNumer =" + dokNag.GIDNumer;
                    numerDok = ConnectToSQL(connectionString, numerDokQuery);
                }




                return numerDok;
            }
            catch (Exception e)
            {
                tw2.WriteLine("Błąd dodawania faktury " + faktura.Nag.Id + ": " + e.Message);
                return "";
            }
        }


        static public int DodajElem(Elem elem, TextWriter tw2, int sesja, int dokId, ref string bladString)
        {
            string twrKodQuery = " SELECT Twr_Kod FROM cdn.TwrKarty WHERE Twr_GIDNumer = " + elem.Gid_numer;
            string twrKod = ConnectToSQL(connectionString, twrKodQuery);

            tw2.WriteLine("Dodawanie pozycji: " + twrKod);

            tw2.WriteLine("Podatek z JSON: " + elem.Podatek);

            XLDokumentElemInfo_20232 XLelem = new XLDokumentElemInfo_20232();
            XLelem.GIDFirma = 0;
            XLelem.GIDLp = 0;
            XLelem.GIDNumer = 0;
            XLelem.GIDTyp = 0;
            XLelem.Wersja = XLWersjaAPI;
            XLelem.TowarKod = twrKod;
            XLelem.CenaP = elem.Cena;
            XLelem.Cena = elem.Cena_rabt;
            XLelem.Ilosc = elem.Ilosc;
            XLelem.StawkaPod = Convert.ToInt32(Convert.ToDouble(elem.Podatek)*100);

            switch(elem.Podatek)
            {
                case "23":
                    {
                        XLelem.Vat = "A";
                        break;
                    }
                case "8":
                    {
                        XLelem.Vat = "B";
                        break;
                    }
                case "0":
                    {
                        XLelem.Vat = "C";
                        break;
                    }
                case "7":
                    {
                        XLelem.Vat = "F";
                        break;
                    }
                case "5":
                    {
                        XLelem.Vat = "G";
                        break;
                    }

            }




            int blad = cdn_api.cdn_api.XLDodajPozycje(dokId, XLelem);
            if (blad != 0)
            {
                tw2.WriteLine("Błąd dodawania elementu " + elem.Gid_numer + ": " + blad);
                bladString = "Błąd dodawania elementu " + elem.Gid_numer + ": " + blad;
                return blad;
            }

            tw2.WriteLine("Podatek na pozycji: " + XLelem.StawkaPod);


            return 0;
        }


        static public string krajKnt(string kraj_id)
        {
            switch (kraj_id)
            {
                case ("1"):
                    {
                        return "Afganistan";

                    }
                case ("2"):
                    {
                        return "Albania";
                    }
                case ("3"):
                    {
                        return "Algieria";
                    }
                case ("4"):
                    {
                        return "Samoa Amerykańskie";
                    }
                case ("5"):
                    {
                        return "Andora";
                    }
                case ("6"):
                    {
                        return "Angola";
                    }
                case ("7"):
                    {
                        return "Anguilla";
                    }
                case ("8"):
                    {
                        return "Antarktyda";
                    }
                case ("9"):
                    {
                        return "Antigua i Barbuda";
                    }
                case ("10"):
                    {
                        return "Argentyna";
                    }
                case ("11"):
                    {
                        return "Armenia";
                    }
                case ("12"):
                    {
                        return "Aruba";
                    }
                case ("13"):
                    {
                        return "Australia";
                    }
                case ("14"):
                    {
                        return "Austria";
                    }
                case ("15"):
                    {
                        return "Azerbejdżan";
                    }
                case ("16"):
                    {
                        return "Bahamy";
                    }
                case ("17"):
                    {
                        return "Bahrajn";
                    }
                case ("18"):
                    {
                        return "Bangladesz";
                    }
                case ("19"):
                    {
                        return "Barbados";
                    }
                case ("20"):
                    {
                        return "Białoruś";
                    }
                case ("21"):
                    {
                        return "Belgia";
                    }
                case ("22"):
                    {
                        return "Belize";
                    }
                case ("23"):
                    {
                        return "Benin";
                    }
                case ("24"):
                    {
                        return "Bermudy";
                    }
                case ("25"):
                    {
                        return "Bhutan";
                    }
                case ("26"):
                    {
                        return "Boliwia";
                    }
                case ("27"):
                    {
                        return "Bośnia i Hercegowina";
                    }
                case ("28"):
                    {
                        return "Botswana";
                    }
                case ("29"):
                    {
                        return "Wyspa Bouveta";
                    }
                case ("30"):

                    {
                        return "Brazylia";
                    }
                case ("31"):
                    {
                        return "Brytyjskie Terytorium Oceanu Indyjskiego";
                    }
                case ("32"):
                    {
                        return "Brunei";
                    }
                case ("33"):
                    {
                        return "Bułgaria";
                    }
                case ("34"):
                    {
                        return "Burkina Faso";
                    }
                case ("35"):
                    {
                        return "Burundi";
                    }
                case ("36"):
                    {
                        return "Kambodża";
                    }
                case ("37"):
                    {
                        return "Kamerun";
                    }
                case ("38"):
                    {
                        return "Kanada";
                    }
                case ("39"):
                    {
                        return "Republika Zielonego Przylądka";
                    }
                case ("40"):
                    {
                        return "Kajmany";
                    }
                case ("41"):
                    {
                        return "Republika Środkowoafrykańska";
                    }
                case ("42"):
                    {
                        return "Czad";
                    }
                case ("43"):
                    {
                        return "Chile";
                    }
                case ("44"):
                    {
                        return "Chiny";
                    }
                case ("45"):
                    {
                        return "Wyspa Bożego Narodzenia";
                    }
                case ("46"):
                    {
                        return "Wyspy Kokosowe";
                    }
                case ("47"):
                    {
                        return "Kolumbia";
                    }
                case ("48"):
                    {
                        return "Komory";
                    }
                case ("49"):
                    {
                        return "Republika Kongo";
                    }
                case ("50"):
                    {
                        return "Wyspy Cooka";
                    }
                case ("51"):
                    {
                        return "Kostaryka";
                    }
                case ("52"):
                    {
                        return "Wybrzeże Kości Słoniowej";
                    }
                case ("53"):
                    {
                        return "Chorwacja";
                    }
                case ("54"):
                    {
                        return "Kuba";
                    }
                case ("55"):
                    {
                        return "Cypr";
                    }
                case ("56"):
                    {
                        return "Czechy";
                    }
                case ("57"):
                    {
                        return "Dania";
                    }
                case ("58"):
                    {
                        return "Dżibuti";
                    }
                case ("59"):
                    {
                        return "Dominika";
                    }
                case ("60"):
                    {
                        return "Dominikana";
                    }
                case ("61"):
                    {
                        return "Timor Wschodni";
                    }
                case ("62"):
                    {
                        return "Ekwador";
                    }
                case ("63"):
                    {
                        return "Egipt";
                    }
                case ("64"):
                    {
                        return "Salwador";
                    }
                case ("65"):
                    {
                        return "Gwinea Równikowa";
                    }
                case ("66"):
                    {
                        return "Erytrea";
                    }
                case ("67"):
                    {
                        return "Estonia";
                    }
                case ("68"):
                    {
                        return "Etiopia";
                    }
                case ("69"):
                    {
                        return "Falklandy";
                    }
                case ("70"):
                    {
                        return "Wyspy Owcze";
                    }
                case ("71"):
                    {
                        return "Fidżi";
                    }
                case ("72"):
                    {
                        return "Finlandia";
                    }
                case ("73"):
                    {
                        return "Francja";
                    }
                case ("74"):
                    {
                        return "Gujana Francuska";
                    }
                case ("75"):
                    {
                        return "Polinezja Francuska";
                    }
                case ("76"):
                    {
                        return "Francuskie Terytoria Południowe";
                    }
                case ("77"):
                    {
                        return "Gabon";
                    }
                case ("78"):
                    {
                        return "Gambia";
                    }
                case ("79"):
                    {
                        return "Gruzja";
                    }
                case ("80"):
                    {
                        return "Niemcy";
                    }
                case ("81"):
                    {
                        return "Ghana";
                    }
                case ("82"):
                    {
                        return "Gibraltar";
                    }
                case ("83"):
                    {
                        return "Grecja";
                    }
                case ("84"):
                    {
                        return "Grenlandia";
                    }
                case ("85"):
                    {
                        return "Grenada";
                    }
                case ("86"):
                    {
                        return "Gwadelupa";
                    }
                case ("87"):
                    {
                        return "Guam";
                    }
                case ("88"):
                    {
                        return "Gwatemala";
                    }
                case ("89"):
                    {
                        return "Gwinea";
                    }
                case ("90"):
                    {
                        return "Gwinea Bissau";
                    }
                case ("91"):
                    {
                        return "Gujana";
                    }
                case ("92"):
                    {
                        return "Haiti";
                    }
                case ("93"):
                    {
                        return "Wyspy Heard i McDonalda";
                    }
                case ("94"):
                    {
                        return "Honduras";
                    }
                case ("95"):
                    {
                        return "Hongkong";
                    }
                case ("96"):
                    {
                        return "Węgry";
                    }
                case ("97"):
                    {
                        return "Islandia";
                    }
                case ("98"):
                    {
                        return "Indie";
                    }
                case ("99"):
                    {
                        return "Indonezja";
                    }
                case ("100"):
                    {
                        return "Iran";
                    }
                case ("101"):
                    {
                        return "Irak";
                    }
                case ("102"):
                    {
                        return "Irlandia";
                    }
                case ("103"):
                    {
                        return "Izrael";
                    }
                case ("104"):
                    {
                        return "Włochy";
                    }
                case ("105"):
                    {
                        return "Jamajka";
                    }
                case ("106"):
                    {
                        return "Japonia";
                    }
                case ("107"):
                    {
                        return "Jordania";
                    }
                case ("108"):
                    {
                        return "Kazachstan";
                    }
                case ("109"):
                    {
                        return "Kenia";
                    }
                case ("110"):
                    {
                        return "Kiribati";
                    }
                case ("111"):
                    {
                        return "Korea Północna";
                    }
                case ("112"):
                    {
                        return "Korea Południowa";
                    }
                case ("113"):
                    {
                        return "Kuwejt";
                    }
                case ("114"):
                    {
                        return "Kirgistan";
                    }
                case ("115"):
                    {
                        return "Laos";
                        
                    }
                case ("116"):
                    {
                        return "Łotwa";
                    }
                case ("117"):
                    {
                        return "Liban";
                    }
                case ("118"):
                    {
                        return "Lesotho";
                    }
                case ("119"):
                    {
                        return "Liberia";
                    }
                case ("120"):
                    {
                        return "Libia";
                    }
                case ("121"):
                    {
                        return "Liechtenstein";
                    }
                case ("122"):
                    {
                        return "Litwa";
                    }
                case ("123"):
                    {
                        return "Luksemburg";
                    }
                case ("124"):
                    {
                        return "Makao";
                    }
                case ("125"):
                    {
                        return "Macedonia Północna";
                    }
                case ("126"):
                    {
                        return "Madagaskar";
                    }
                case ("127"):
                    {
                        return "Malawi";
                    }
                case ("128"):
                    {
                        return "Malezja";
                    }
                case ("129"):
                    {
                        return "Malediwy";
                    }
                case ("130"):
                    {
                        return "Mali";
                    }
                case ("131"):
                    {
                        return "Malta";
                    }
                case ("132"):
                    {
                        return "Wyspy Marshalla";
                    }
                case ("133"):
                    {
                        return "Martynika";
                    }
                case ("134"):
                    {
                        return "Mauretania";
                    }
                case ("135"):
                    {
                        return "Mauritius";
                    }
                case ("136"):
                    {
                        return "Majotta";
                    }
                case ("137"):
                    {
                        return "Meksyk";
                    }
                case ("138"):
                    {
                        return "Mikronezja";
                    }
                case ("139"):
                    {
                        return "Mołdawia";
                    }
                case ("140"):
                    {
                        return "Monako";
                    }
                case ("141"):
                    {
                        return "Mongolia";
                    }
                case ("142"):
                    {
                        return "Montserrat";
                    }
                case ("143"):
                    {
                        return "Maroko";
                    }
                case ("144"):
                    {
                        return "Mozambik";
                    }
                case ("145"):
                    {
                        return "Mjanma";
                    }
                case ("146"):
                    {
                        return "Namibia";
                    }
                case ("147"):
                    {
                        return "Nauru";
                    }
                case ("148"):
                    {
                        return "Nepal";
                    }
                case ("149"):
                    {
                        return "Holandia";
                    }
                case ("150"):
                    {
                        return "Antyle Holenderskie";
                    }
                case ("152"):
                    {
                        return "Nowa Kaledonia";
                    }
                case ("153"):
                    {
                        return "Nowa Zelandia";
                    }
                case ("154"):
                    {
                        return "Nikaragua";
                    }
                case ("155"):
                    {
                        return "Niger";
                    }
                case ("156"):
                    {
                        return "Nigeria";
                    }
                case ("157"):
                    {
                        return "Niue";
                    }
                case ("158"):
                    {
                        return "Wyspa Norfolk";
                    }
                case ("159"):
                    {
                        return "Mariany Północne";
                    }
                case ("160"):
                    {
                        return "Norwegia";
                    }
                case ("161"):
                    {
                        return "Oman";
                    }
                case ("162"):
                    {
                        return "Pakistan";
                    }
                case ("163"):
                    {
                        return "Palau";
                    }
                case ("164"):
                    {
                        return "Panama";
                    }
                case ("165"):
                    {
                        return "Papua - Nowa Gwinea";
                    }
                case ("166"):
                    {
                        return "Paragwaj";
                    }
                case ("167"):
                    {
                        return "Peru";
                    }
                case ("168"):
                    {
                        return "Filipiny";
                    }
                case ("169"):
                    {
                        return "Pitcairn";
                    }
                case ("170"):
                    {
                        return "Polska";
                    }
                case ("171"):
                    {
                        return "Portugalia";
                    }
                case ("172"):
                    {
                        return "Portoryko";
                    }
                case ("173"):
                    {
                        return "Katar";
                    }
                case ("174"):
                    {
                        return "Zjazd";
                    }
                case ("175"):
                    {
                        return "Rumunia";
                    }
                case ("176"):
                    {
                        return "Rosja";
                    }
                case ("177"):
                    {
                        return "Rwanda";
                    }
                case ("178"):
                    {
                        return "Georgia Południowa i Sandwich Południowy";
                    }
                case ("179"):
                    {
                        return "Saint Kitts i Nevis";
                    }
                case ("180"):
                    {
                        return "Saint Lucia";
                    }
                case ("181"):
                    {
                        return "Saint Vincent i Grenadyny";
                    }
                case ("182"):
                    {
                        return "Samoa";
                    }
                case ("183"):
                    {
                        return "San Marino";
                    }
                case ("184"):
                    {
                        return "Wyspy Świętego Tomasza i Książęca";
                    }
                case ("185"):
                    {
                        return "Arabia Saudyjska";
                    }
                case ("186"):
                    {
                        return "Senegal";
                    }
                case ("187"):
                    {
                        return "Seszele";
                    }
                case ("188"):
                    {
                        return "Sierra Leone";
                    }
                case ("189"):
                    {
                        return "Singapur";
                    }
                case ("190"):
                    {
                        return "Słowacja";
                    }
                case ("191"):
                    {
                        return "Słowenia";
                    }
                case ("192"):
                    {
                        return "Wyspy Salomona";
                    }
                case ("193"):
                    {
                        return "Somalia";
                    }
                case ("194"):
                    {
                        return "Południowa Afryka";
                    }
                case ("195"):
                    {
                        return "Hiszpania";
                    }
                case ("196"):
                    {
                        return "Sri Lanka";
                    }
                case ("197"):
                    {
                        return "Święta Helena";
                    }
                case ("198"):
                    {
                        return "Saint - Pierre i Miquelon";
                    }
                case ("199"):
                    {
                        return "Sudan";
                    }
                case ("200"):
                    {
                        return "Svalbard i Jan Mayen";
                    }
                case ("201"):
                    {
                        return "Svalbard i Jan Mayen";
                    }
                case ("202"):
                    {
                        return "Eswatini";
                    }
                case ("203"):
                    {
                        return "Szwecja";
                    }
                case ("204"):
                    {
                        return "Szwajcaria";
                    }
                case ("205"):
                    {
                        return "Syria";
                    }
                case ("206"):
                    {
                        return "Tajwan";
                    }
                case ("207"):
                    {
                        return "Tadżykistan";
                    }
                case ("208"):
                    {
                        return "Tanzania";
                    }
                case ("209"):
                    {
                        return "Tajlandia";
                    }
                case ("210"):
                    {
                        return "Togo";
                    }
                case ("211"):
                    {
                        return "Tokelau";
                    }
                case ("212"):
                    {
                        return "Tonga";
                    }
                case ("213"):
                    {
                        return "Trynidad i Tobago";
                    }
                case ("214"):
                    {
                        return "Tunezja";
                    }
                case ("215"):
                    {
                        return "Turcja";
                    }
                case ("216"):
                    {
                        return "Turkmenistan";
                    }
                case ("217"):
                    {
                        return "Wyspy Turks i Caicos";
                    }
                case ("218"):
                    {
                        return "Tuvalu";
                    }
                case ("219"):
                    {
                        return "Stany Zjednoczone Dalekie Wyspy Mniejsze";
                    }
                case ("220"):
                    {
                        return "Uganda";
                    }
                case ("221"):
                    {
                        return "Ukraina";
                    }
                case ("222"):
                    {
                        return "Zjednoczone Emiraty Arabskie";
                    }
                case ("223"):
                    {
                        return "Wielka Brytania";
                    }
                case ("224"):
                    {
                        return "Stany Zjednoczone";
                    }
                case ("225"):
                    {
                        return "Urugwaj";
                    }
                case ("226"):
                    {
                        return "Uzbekistan";
                    }
                case ("227"):
                    {
                        return "Vanuatu";
                    }
                case ("228"):
                    {
                        return "Watykan";
                    }
                case ("229"):
                    {
                        return "Wenezuela";
                    }
                case ("230"):
                    {
                        return "Wietnam";
                    }
                case ("231"):
                    {
                        return "Brytyjskie Wyspy Dziewicze";
                    }
                case ("232"):
                    {
                        return "Wyspy Dziewicze Stanów Zjednoczonych";
                    }
                case ("233"):
                    {
                        return "Wallis i Futuna";
                    }
                case ("234"):
                    {
                        return "Sahara Zachodnia";
                    }
                case ("235"):
                    {
                        return "Jemen";
                    }
                case ("237"):
                    {
                        return "Demokratyczna Republika Konga";
                    }
                case ("238"):
                    {
                        return "Zambia";
                    }
                case ("239"):
                    {
                        return "Zimbabwe";
                    }
                case ("240"):
                    {
                        return "Serbia";
                    }
                case ("241"):
                    {
                        return "Czarnogóra";
                    }
                case ("242"):
                    {
                        return "Kosowo";
                    }
                case ("244"):
                    {
                        return "Wyspy Alandzkie";
                    }
                case ("245"):
                    {
                        return "Święty Barthelemy";
                    }
                case ("246"):
                    {
                        return "Bonaire, Święty Eustatius i Saba";
                    }
                case ("247"):
                    {
                        return "Curacao";
                    }
                case ("248"):
                    {
                        return "Guernsey";
                    }
                case ("249"):
                    {
                        return "Wyspa Man";
                    }
                case ("250"):
                    {
                        return "Golf";
                    }
                case ("251"):
                    {
                        return "Święty Marcin";
                    }
                case ("252"):
                    {
                        return "Teretorium Paleństynskie";
                    }
                case ("253"):
                    {
                        return "Sudan Południowy";
                    }
                case ("254"):
                    {
                        return "St Martin";
                    }

            }

            return "Polska";
        }



        static public string ConnectToSQL(string connection_string, string query)
        {
            string str = "";
            try
            {
                using (SqlConnection connection = new SqlConnection(connection_string))
                {
                    SqlDataAdapter adapter = new SqlDataAdapter();
                    adapter.SelectCommand = new SqlCommand(query, connection);
                    DataSet dataSet = new DataSet();
                    adapter.Fill(dataSet, "Query");
                    if (dataSet.Tables.Count == 0)
                    {
                        return "";
                    }
                    foreach (DataRow row in (InternalDataCollectionBase)dataSet.Tables[0].Rows)
                    {
                        str = row[0].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                str = str + "Bład SQL:" + ex.ToString() + "\n";
            }
            return str;
        }
    }

    
}
