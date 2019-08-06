using Ks.Fiks.Maskinporten.Client;
using KS.Fiks.IO.Client;
using KS.Fiks.IO.Client.Configuration;
using KS.Fiks.IO.Client.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace ks.fiks.io.eplansak.sample
{
    class Program
    {
        static void Main(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder()
            
            .AddJsonFile("appsettings.json", true, true)
            .AddJsonFile("appsettings.development.json", true, true)
            .Build();


            //https://github.com/ks-no/fiks-io-client-dotnet

            Console.WriteLine("Setter opp FIKS IO integrasjon for ePlansak...");

            Guid accountId = Guid.Parse(config["accountId"]);  /* Fiks IO accountId as Guid Banke kommune digitalt planregister konto*/
            string privateKey = File.ReadAllText("privkey.pem"); ; /* Private key supplied to Fiks IO account */
            Guid integrationId = Guid.Parse(config["integrationId"]); /* Integration id as Guid ePlansak system X */
            string integrationPassword = config["integrationPassword"];  /* Integration password */

            // Fiks IO account configuration
            var account = new KontoConfiguration(
                                accountId,
                                privateKey);

            // Id and password for integration associated to the Fiks IO account.
            var integration = new IntegrasjonConfiguration(
                                    integrationId,
                                    integrationPassword, "ks:fiks");

            // ID-porten machine to machine configuration
            var maskinporten = new MaskinportenClientConfiguration(
                audience: @"https://oidc-ver2.difi.no/idporten-oidc-provider/", // ID-porten audience path
                tokenEndpoint: @"https://oidc-ver2.difi.no/idporten-oidc-provider/token", // ID-porten token path
                issuer: @"arkitektum_test",  // issuer name
                numberOfSecondsLeftBeforeExpire: 10, // The token will be refreshed 10 seconds before it expires
                certificate: GetCertificate(config["ThumbprintIdPortenVirksomhetssertifikat"]));

            // Optional: Use custom api host (i.e. for connecting to test api)
            var api = new ApiConfiguration(
                            scheme: "https",
                            host: "api.fiks.test.ks.no",
                            port: 443);

            // Optional: Use custom amqp host (i.e. for connection to test queue)
            var amqp = new AmqpConfiguration(
                            host: "io.fiks.test.ks.no",
                            port: 5671);

            // Combine all configurations
            var configuration = new FiksIOConfiguration(account, integration, maskinporten, api, amqp);

            Guid receiverId = accountId; // Receiver id as Guid
            Guid senderId = accountId; // Sender id as Guid


            var client = new FiksIOClient(configuration); // See setup of configuration below
            var messageRequestOpprettePlan = new MeldingRequest(
                                        mottakerKontoId: receiverId,
                                        avsenderKontoId: senderId,
                                        meldingType: "no.ks.geointegrasjon.plan.oppretteplanidentinput.v1"); // Message type as string https://fiks.ks.no/plan.oppretteplanidentinput.v1.schema.json
            //Se oversikt over meldingstyper på https://github.com/ks-no/fiks-io-meldingstype-katalog/tree/test/schema

            //var onReceived = new EventHandler<MottattMeldingArgs>((sender, fileArgs) =>
            //{
            //    using (var archiveAsStream = fileArgs.Melding.DecryptedStream)
            //    {
            //        // Process the stream
            //        StreamReader reader = new StreamReader(archiveAsStream.Result);
            //        string text = reader.ReadToEnd();
            //    }
            //    Console.WriteLine("Mottar melding...");
            //    //fileArgs.SvarSender.Ack(); // Ack message if handling of stream succeeded to remove it from the queue
            //});

            //client.NewSubscription(onReceived);

            string payload = File.ReadAllText("sampleOpprettePlan.json");
            // Sending a string
            Console.WriteLine("Sender melding...");
            var msg = client.Send(messageRequestOpprettePlan, payload, "meldingOmNyPlan.json").Result;
            Console.WriteLine("Melding " + msg.MeldingId.ToString() + " sendt..." + msg.MeldingType);
            


        }

        private static X509Certificate2 GetCertificate(string ThumbprintIdPortenVirksomhetssertifikat)
        {

            //Det samme virksomhetssertifikat som er registrert hos ID-porten
            X509Store store = new X509Store(StoreLocation.CurrentUser);
            X509Certificate2 cer = null;
            store.Open(OpenFlags.ReadOnly);
            //Henter Arkitektum sitt virksomhetssertifikat
            X509Certificate2Collection cers = store.Certificates.Find(X509FindType.FindByThumbprint, ThumbprintIdPortenVirksomhetssertifikat, false);
            if (cers.Count > 0)
            {
                cer = cers[0];
            };
            store.Close();

            return cer;
        }
    }
}
