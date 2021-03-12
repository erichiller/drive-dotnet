using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Discovery.v1;
using Google.Apis.Discovery.v1.Data;
using Google.Apis.Drive.v3;
using Google.Apis.Services;

/*

Service accounts aren't automatically connected to anything. Even if you made them on your personal google account, they aren't really related to that account - aside from that account having the permissions to change or delete them. You can kind of think of them as being their own google accounts except missing a few components. They have their own email addresses ect.

https://forum.rclone.org/t/service-account-not-allowing-to-see-files-and-folders/12527/9

Samples

https://developers.google.com/drive/api/v3/quickstart/dotnet
https://github.com/googleworkspace/dotnet-samples/blob/master/drive/DriveQuickstart/DriveQuickstart.cs

Create a service account

https://developers.google.com/identity/protocols/oauth2/service-account#creatinganaccount

*/
namespace drive_dotnet {

    /// <summary>
    /// This example uses the discovery API to list all APIs in the discovery repository.
    /// https://developers.google.com/discovery/v1/using.
    /// <summary>
    class Program {
        [STAThread]
        static void Main( string[] args ) {
            Console.WriteLine( "Google Drive API" );
            Console.WriteLine( "====================" );
            // arg 0: file name to download
            // arg 1: path to download to
            // arg 2: credentials file
            foreach ( var arg in args ) {
                Console.WriteLine( $"arg: {arg}" );
            }
            var fileName = String.Empty;                    // arg[0]
            var outputPath = String.Empty;                  // arg[1]
            var credentialsFile = ".credentials.json";      // arg[2]
            if (args.Length == 0){
                Console.WriteLine( "no filename detected" );
                return;
            } else {
                fileName = args[ 0 ];
                Console.WriteLine($"Looking for {fileName}");
            }
            if( args.Length > 1 ){
                outputPath = args[1];
                Console.WriteLine( $"Will output to {outputPath}");
            }
            if ( args.Length > 2 ){
                credentialsFile = args[2];
                Console.WriteLine($"Using credentials in Path {credentialsFile}");
            }
            try {
                var actions = new DriveActions( credentialsFile );
                // actions.GetDrives().Wait();
                // actions.GetFiles();
                // actions.GetAbout();

                // actions.GetSpreadsheetAsOds("1fmOLsbB_yQgFcYlBeeAqxv0-72O5WEy4kRRjdDhVpr0").Wait();

                // actions.GetSpreadsheetAsOdsByName( "macd" ).Wait();
                actions.GetSpreadsheetAsOdsByName( fileName: fileName, outputPath ).Wait();

            } catch ( AggregateException ex ) {
                foreach ( var e in ex.InnerExceptions ) {
                    Console.WriteLine( "ERROR: " + e.Message );
                }
            }
            // Console.WriteLine( "Press any key to continue..." );
            // Console.ReadKey();
        }

    }
    public class DriveActions {
        DriveService service;

        public GoogleCredential credential {
            get {

                // string exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? throw new DirectoryNotFoundException("Unable to locate the ExecutingAssembly's location, configuration failed.");
                // string accessCredentialsPath = Path.Join(exeDir, this.credentialsFile);
                string accessCredentialsPath = this.credentialsFile;
                if ( !System.IO.File.Exists( accessCredentialsPath ) ) {
                    throw new Exception( $"ERROR: file at {accessCredentialsPath} does not exist!" );
                }
                return GoogleCredential
                            .FromFile( accessCredentialsPath )
                            .CreateScoped(
                                // https://developers.google.com/identity/protocols/oauth2/scopes
                                new string[]{
                                    // "DriveService.Scope.DriveReadonly"
                                    "https://www.googleapis.com/auth/drive",
                                    "https://www.googleapis.com/auth/drive.install",
                                    "https://www.googleapis.com/auth/drive.file",
                                    "https://www.googleapis.com/auth/drive.appdata",
                                    // "https://www.googleapis.com/auth/drive.metadata	"
                                    // "https://www.googleapis.com/auth/spreadsheets"
                                } );
            }
        }
        private string credentialsFile;

        public DriveActions( string credentialsFile) {
            this.credentialsFile = credentialsFile;
            Console.WriteLine($"Using credentials at {this.credentialsFile}");
            service = new DriveService( new BaseClientService.Initializer {
                ApplicationName = "dotnet-drive",
                HttpClientInitializer = credential,
            } );
        }


        public async Task GetServices( ) {
            // Create the service.
            var service = new DiscoveryService(new BaseClientService.Initializer
            {
                ApplicationName = "dotnet-drive",
            });

            // Run the request.
            Console.WriteLine( "Executing a list request..." );
            var result = await service.Apis.List().ExecuteAsync();

            // Display the results.
            if ( result.Items != null ) {
                foreach ( DirectoryList.ItemsData api in result.Items ) {
                    Console.WriteLine( api.Id + " - " + api.Title );
                }
            }
        }

        public async Task GetSpreadsheetAsOdsByName( string fileName, string? outputBasePath = null ) {

            FilesResource.ListRequest listRequest = service.Files.List();
            listRequest.PageSize = 10;
            listRequest.Fields = "nextPageToken, files(id, name, mimeType)";
            // https://developers.google.com/drive/api/v3/search-files
            listRequest.Q = $"name = '{fileName}'";
            var files = await listRequest.ExecuteAsync();
            if ( files.Files.Count != 1 ) {
                throw new Exception( $"Files found invalid amount = {files.Files.Count}" );
            }
            await GetSpreadsheetAsOds( files.Files.Single().Id, outputBasePath );
        }

        public async Task GetSpreadsheetAsOds( string fileId, string? outputBasePath = null ) {
            const string odsMimeType = "application/x-vnd.oasis.opendocument.spreadsheet";
            // Define parameters of request.
            var fileRequest = service.Files.Get(fileId);
            fileRequest.Fields = "id, name, mimeType, exportLinks";
            var file = await fileRequest.ExecuteAsync();

            var exported_file = await service.Files.Export( fileId, odsMimeType ).ExecuteAsStreamAsync();

            // service.Files.Get(fileId).MediaDownloader.DownloadAsync(
            using var f = File.Open( Path.Join( outputBasePath ?? "", $"{file.Name}.ods" ), FileMode.Create );
            
            await exported_file.CopyToAsync( f );
            await f.FlushAsync();
            f.Close();
        }

        public async Task GetFileExportUrls( string fileId ) {

            var fileRequest = service.Files.Get(fileId);
            fileRequest.Fields = "id, name, mimeType, kind, exportLinks";
            var file = await fileRequest.ExecuteAsync();
            if ( file.ExportLinks.Keys.Contains( "application/vnd.oasis.opendocument.spreadsheet" ) ) {
                var odsLink = file.ExportLinks["application/vnd.oasis.opendocument.spreadsheet"];
                Console.WriteLine( "export links: {0}", odsLink );

                using ( var client = new System.Net.WebClient() ) {
                    // var token = credential.GetOidcTokenAsync();
                    var token = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
                    client.Headers.Set( System.Net.HttpRequestHeader.Authorization, $"Bearer {token}" );
                    client.DownloadFile( odsLink, file.Name + ".ods" );
                }


            }
        }

        public void GetAbout( ) {
            // Define parameters of request.
            var about = service.About.Get();
            about.Fields = "*";
            var aboutResult = about.Execute().User
            .EmailAddress
            // .DisplayName
            ;
            Console.WriteLine( $"About ({aboutResult}):" );
        }

        public void GetFiles( ) {
            // Define parameters of request.
            FilesResource.ListRequest listRequest = service.Files.List();
            listRequest.PageSize = 10;
            listRequest.Fields = "nextPageToken, files(id, name, mimeType, exportLinks)";

            // List files.
            IList<Google.Apis.Drive.v3.Data.File> files = listRequest.Execute()
                .Files;
            Console.WriteLine( $"Files ({files.Count}):" );
            if ( files != null && files.Count > 0 ) {
                foreach ( var file in files ) {
                    Console.WriteLine( "{0} ({1})", file.Name, file.Id );
                    Console.WriteLine( "\tcount: {0}", file.ExportLinks?.Count );
                    if ( file.ExportLinks is IDictionary<string, string> links ) {

                        foreach ( var link in links ) {
                            Console.WriteLine( $"\t\t{link.Key} : {link.Value}" );
                        }

                    }
                    Console.WriteLine( "\tcount: {0}", file.ExportLinks?.Count );
                    Console.WriteLine( "\tkind: {0}", file.Kind );
                    Console.WriteLine( "\tmime: {0}", file.MimeType );
                    Console.WriteLine( "\tprops: {0}", file.Properties );
                    Console.WriteLine( $"\tsize: {file.Size}" );
                    Console.WriteLine( $"\tname: {file.Name}" );
                    Console.WriteLine( $"\tcreated: {file.CreatedTime}" );
                }
            } else {
                Console.WriteLine( "No files found." );
            }
        }
        public async Task GetDrives( ) {
            var drives = await service.Drives.List().ExecuteAsync();
            Console.WriteLine( $"Drives ({drives.Drives.Count}) :" );
            if ( drives != null && drives.Drives.Count > 0 ) {
                foreach ( var drive in drives.Drives ) {
                    Console.WriteLine( "{0} ({1})", drive.Name, drive.Id );
                }
            } else {
                Console.WriteLine( "No drives found." );
            }
            // var getRequest = service.Objects.Get("BUCKET_HERE", "OBJECT_HERE");
            // using (var fileStream = new System.IO.FileStream(
            //     "FILE_PATH_HERE",
            //     System.IO.FileMode.Create,
            //     System.IO.FileAccess.Write))
            // {
            //     // Add a handler which will be notified on progress changes.
            //     // It will notify on each chunk download and when the
            //     // download is completed or failed.
            //     getRequest.MediaDownloader.ProgressChanged += Download_ProgressChanged;
            //     getRequest.Download(fileStream);
            // }
        }
    }
}