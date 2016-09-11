using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace MyTMobileUWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;

        string capiUrl = "https://capi.t-mobile.nl";

        public async void GetOAuth2Token()
        {
            string startURL = "https://www.t-mobile.nl/oauth2/authorize?response_type=code&client_id=gun182f1jyacn13f&scope=usage+readfinancial+readsubscription+readpersonal+readloyalty&redirect_uri=https%3A%2F%2Fapps.fits4all.net%2Ftmobileuwp%2Foauth2%2Fcallback";
            string endURL = "https://apps.fits4all.net/tmobileuwp/oauth2/callback"; // This is where we get the URL that we use to request the access token.

            System.Uri startURI = new System.Uri(startURL);
            System.Uri endURI = new System.Uri(endURL);

            try
            {
                var webAuthenticationResult =
                    await Windows.Security.Authentication.Web.WebAuthenticationBroker.AuthenticateAsync(
                    Windows.Security.Authentication.Web.WebAuthenticationOptions.None,
                    startURI,
                    endURI);

                switch (webAuthenticationResult.ResponseStatus)
                {
                    case Windows.Security.Authentication.Web.WebAuthenticationStatus.Success:
                        // Successful authentication.
                        string url = webAuthenticationResult.ResponseData.ToString();
                        //string accesstoken = getJSON(url);
                        // Get the access token now.
                        getAccessToken(url);
                        break;
                    case Windows.Security.Authentication.Web.WebAuthenticationStatus.ErrorHttp:
                        // HTTP error. 
                        ResultTextBlock.Text = "Er is een fout opgetreden tijdens het laden van de T-Mobile website. Foutmelding: " + webAuthenticationResult.ResponseErrorDetail.ToString();
                        break;
                    default:
                        // Other error.
                        ResultTextBlock.Text = "Er kan geen verbinding worden gemaakt met de T-Mobile website. Probeer het opnieuw." + webAuthenticationResult.ResponseData.ToString();
                        break;
                }
            }
            catch (Exception ex)
            {
                // Authentication failed. Handle parameter, SSL/TLS, and Network Unavailable errors here. 
                ResultTextBlock.Text = "Er kan geen verbinding worden gemaakt met de T-Mobile website. Foutmelding: " + ex.Message;
            }
            
        }

        // Use this function to store a setting to the app's Local Settings (we use this to store the OAuth2Token, for example)
        public void StoreSetting(string key, string value)
        {
            localSettings.Values[key] = value;
        }

        public MainPage()
        {
            this.InitializeComponent();

            if (!localSettings.Values.ContainsKey("OAuth2Token"))
            {
                // User hasn't been authenticated yet. Let's ask him to login using the My T-Mobile website.
                GetOAuth2Token();
            }
            else
            {
                // User has authenticated himself in the past so we have his OAuth2Token already.
                ResultTextBlock.Text += "\nOAuth2Token is " + localSettings.Values["OAuth2Token"];
            }
        }

        public async void getAccessToken(string url)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            try
            {
                var response = await httpClient.GetAsync(new Uri(url));
                string jsonString = await response.Content.ReadAsStringAsync();
                JsonObject rootValue = JsonObject.Parse(jsonString);

                if (rootValue.ContainsKey("access_token"))
                {
                    string token = rootValue["access_token"].GetString();

                    if (token != "")
                    {
                        // Done. Store the token to local settings.
                        StoreSetting("OAuth2Token", token);
                    }
                    else
                    {
                        ResultTextBlock.Text += "An access token should have been provided by the server, but the response is empty. Please try again.";
                    }
                }
                else if (rootValue.ContainsKey("error"))
                {
                    if (rootValue.ContainsKey("error_description"))
                    {
                        ResultTextBlock.Text += "An error occured while getting your access token from the server. Details:\n"
                            + rootValue["error"].GetString() + "\n"
                            + rootValue["error_description"].GetString() + "\n";
                    }
                    else
                    {
                        ResultTextBlock.Text += "An error occured while getting your access token from the server, but no details have been specified. The error code is " + rootValue["error"].GetString();
                    }
                }
                else
                {
                    ResultTextBlock.Text += "The server returned an unknown response (no access_token or error). Please try again.";
                }
            }

            catch (FormatException fex)
            {
                //Invalid json format
                ResultTextBlock.Text += "The server provided a non-JSON reply. Please try again.";
            }
            catch (Exception ex) //some other exception
            {
                ResultTextBlock.Text += "An error occured while getting your access token from the server. Error:" + ex.ToString() + "\nURL: "+url;
            }
        }


        public async void getJSON()
        {
            var httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + localSettings.Values["OAuth2Token"].ToString());
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var response = await httpClient.GetAsync(new Uri(capiUrl + "/account/current?resourcelabel=Customer&resourcelabel=Subscription"));
            var jsonString = await response.Content.ReadAsStringAsync();

            JsonObject rootValue = JsonObject.Parse(jsonString);

            if (rootValue.ContainsKey("Username"))
            {
                ResultTextBlock.Text += "Gebruikersnaam: " + rootValue["Username"].GetString() + "\n";
            }

            if (rootValue.ContainsKey("Resources"))
            {
                // First, we'll check which type of customer this is.
                foreach (var resources in rootValue["Resources"].GetArray())
                {
                    var resource = resources.GetObject();
                    if (resource["Label"].Equals("Customer"))
                    {
                        ResultTextBlock.Text += "Customer URL is " + resource["Url"] + "\n";
                    }
                }
            }

            //ResultTextBlock.Text = rootValue["BalanceDate"].GetString();

                /*
                foreach (var bundle in rootValue["Bundles"].GetArray())
                {

                    var unnamedObject = bundle.GetObject();
                    var Name = unnamedObject["Name"].GetString();
                    var Buckets = unnamedObject["Buckets"].GetArray();

                    foreach (var bucket in Buckets)
                    {
                        var unnamedObject2 = bucket.GetObject();
                        var RemainingValuePresentation = unnamedObject2["RemainingValuePresentation"].GetString();
                        var LimitValuePresentation = unnamedObject2["LimitValuePresentation"].GetString();

                        ResultTextBlock.Text = "De naam van je bundel is " + Name + ".\nJe hebt nog " + RemainingValuePresentation + " van " + LimitValuePresentation + " over.";
                    }

                   /* System.Diagnostics.Debug.WriteLine(personObject["name"].GetString());
                    System.Diagnostics.Debug.WriteLine(personObject["country"].GetString());
                    System.Diagnostics.Debug.WriteLine(personObject["city"].GetString());
                    System.Diagnostics.Debug.WriteLine(personObject["phone"].GetString());*/
           // }

        }

    
        private void HamburgerButton_Click(object sender, RoutedEventArgs e)
        {
            MySplitView.IsPaneOpen = !MySplitView.IsPaneOpen;
        }

        private void IconsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            getJSON();
            /*if (OverviewListBoxItem.IsSelected) { ResultTextBlock.Text = "Uw My T-Mobile token is " + localSettings.Values["OAuth2Token"].ToString(); }
            else if (UsageStatusListBoxItem.IsSelected) { ResultTextBlock.Text = "Verbruik komt hier"; }*/
        }
    }
}
