using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using IO = System.IO;

using UnityEngine;
using UnityEditor;

using Pngcs.Unity;

namespace ElevationMapCreator
{
    public class ElevationMapCreatorCore : System.IDisposable
    {
        #region FIELDS

        
        HttpClient _client = new HttpClient();

        
        #endregion
        #region PUBLIC METHODS


        public async Task<string> HTTP_POST
        (
            IElevationServiceProvider serviceProvider ,
            Stack<Coordinate> coordinates ,
            string apikey ,
            int maxCoordinatesPerRequest ,
            Ticket ticket ,
            bool logTraffic
        )
        {
            //assertions:
            if( coordinates.Count==0 )
            {
                Debug.LogWarning("coordinates.Count is 0");
                return null;
            }

            //
            List<Coordinate> requestList = new List<Coordinate>();
            //const int lengthLimit = 102375;//(bytes) this number is guesstimation, i found errors but no documentation on this
            int limit = maxCoordinatesPerRequest;//2200;//guesstimation
            for( int i=0 ; i<limit && coordinates.Count!=0 ; i++ )
            {
                requestList.Add( coordinates.Pop() );
            }
            
            //create the HttpContent for the form to be posted:
            System.Net.Http.ByteArrayContent requestContent = null;
            if( serviceProvider.httpMethod==HttpMethod.Post )
            {
                string json = serviceProvider.GetRequestContent( requestList );
                byte[] buffer = System.Text.Encoding.ASCII.GetBytes( json );
                requestContent = new ByteArrayContent( buffer );
                requestContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue( "application/json" );
                //Debug.Log( "requestContent.Headers.ContentLength.Value: " + requestContent.Headers.ContentLength.Value );

                //log request:
                if( logTraffic ) { Debug.Log( $"requesting { requestList.Count } values:\n{ json }" ); }
            }

            //get the stream of the content.
            string result = null;
            while( result==null && ticket.valid )
            {
                try
                {
                    HttpResponseMessage response;

                    if( serviceProvider.httpMethod==HttpMethod.Post )
                    {
                        response = await _client.PostAsync(
                            serviceProvider.RequestUri( null , apikey ) ,
                            requestContent
                        );
                    }
                    else if( serviceProvider.httpMethod==HttpMethod.Get )
                    {
                        response = await _client.GetAsync(
                            serviceProvider.RequestUri( serviceProvider.GetRequestContent( requestList ) , apikey )
                        );
                    }
                    else
                    {
                        throw new System.NotImplementedException();
                    }
                    
                    result = await response.Content.ReadAsStringAsync();

                    if( result.StartsWith( "<html>" ) )
                    {
                        const string ERROR_504 = "504 Gateway Time-out";
                        const string ERROR_502 = "502 Bad Gateway";
                        const string ERROR_500 = "500 Internal Server Error";
                        
                        //log warning:
                        if( result.Contains(ERROR_504) ) { Debug.LogWarning( ERROR_504 ); }
                        else if( result.Contains(ERROR_502) ) { Debug.LogWarning( ERROR_502 ); }
                        else if( result.Contains(ERROR_500) ) { Debug.LogWarning( ERROR_500 ); }
                        else { Debug.LogWarning( $"invalid response:\n{ result }" ); }

                        //invalidate:
                        result = null;

                        await Task.Delay( 1000 );//try again after delay
                    }
                    else if ( result.StartsWith( "{\"error\": \"Invalid JSON.\"}" ))
                    {
                        //log warning:
                        Debug.LogWarning( $"invalid JSON. Try decreasing { nameof(maxCoordinatesPerRequest) }" );

                        await Task.Delay( 1000 );//try again after delay
                    }
                }
                catch ( System.Net.WebException ex )
                {
                    Debug.LogException( ex );
                    await Task.Delay( 1000 );//try again after delay
                }
                catch ( System.Exception ex )
                {
                    Debug.LogException( ex );
                    await Task.Delay( 1000 );//try again after delay
                }
            }

            //log response:
            if( logTraffic ) { Debug.Log( $"\tresponse:\n{ result }" ); }

            //return results:
            return result;
        }
        
        public async Task GetElevationData
        (
            string filePath ,
            IElevationServiceProvider serviceProvider ,
            string apikey ,
            Ticket<float> ticket ,
            Coordinate start ,
            Coordinate end ,
            CoordinateInt resolution ,
            int maxCoordinatesPerRequest ,
            System.Action repaintWindowCallback ,
            System.Action onFinish ,
            bool logTraffic
        )
        {
            Debug.Log( $"{ nameof(GetElevationData) }() started" );
            
            IO.FileStream stream = null;
            IO.StreamWriter writer = null;

            try
            {
                int skip;

                //open file stream:
                {
                    IO.FileMode fileMode;
                    if(
                        IO.File.Exists( filePath )
                        && EditorUtility.DisplayDialog(
                            "Decide" ,
                            "CONTINUE: Setting must match to succeessfully continue\nOVERWRITE: data will be lost",
                            "CONTINUE" ,
                            "OVERWRITE"
                        )
                    )
                    {
                        fileMode = IO.FileMode.Append;
                        skip = 0;
                        ForEachLine(
                            filePath ,
                            ( line )=> skip++
                        );
                    }
                    else
                    {
                        fileMode = IO.FileMode.Create;
                        skip = 0;
                    }
                    
                    stream = new IO.FileStream(
                        filePath ,
                        fileMode ,
                        IO.FileAccess.Write ,
                        IO.FileShare.Read ,
                        4096 ,
                        IO.FileOptions.SequentialScan
                    );
                    writer = new IO.StreamWriter( stream );
                }

                //populate stack:
                Stack<Coordinate> coordinates = new Stack<Coordinate>();
                {
                    //NOTE: i bet this wont work for ranges overlaping -180/180 latitude crossline etc
                    Coordinate origin = new Coordinate {
                        longitude = Mathf.Min( start.longitude , end.longitude ) ,
                        latitude = Mathf.Min( start.latitude , end.latitude )
                    };
                    
                    float stepX = Mathf.Abs( end.longitude - start.longitude ) / (float)resolution.longitude;
                    float stepY = Mathf.Abs( end.latitude - start.latitude ) / (float)resolution.latitude;
                    for( int Y=0 ; Y<resolution.latitude ; Y++ )
                    for( int X=0 ; X<resolution.longitude ; X++ )
                    {
                        coordinates.Push(
                            new Coordinate {
                                latitude = origin.latitude + (float)Y * stepY ,
                                longitude = origin.longitude + (float)X * stepX
                            }
                        );
                    }
                }
                int numStartingCoordinates = coordinates.Count;

                //skip entries (if applies):
                if( skip!=0 )
                {
                    Debug.Log( $"Skipping { skip } entries" );
                    for( int i=0 ; i<skip ; i++ )
                    {
                        coordinates.Pop();
                    }
                }

                //process stack in batches:
                List<float> elevations = new List<float>( maxCoordinatesPerRequest );
                while( coordinates.Count!=0 )
                {
                    //call api:
                    {
                        //get response:
                        string response = await HTTP_POST(
                            serviceProvider:            serviceProvider ,
                            coordinates:                coordinates ,
                            apikey:                     apikey ,
                            maxCoordinatesPerRequest:   maxCoordinatesPerRequest ,
                            ticket:                     ticket ,
                            logTraffic:                 logTraffic
                        );
                        
                        //
                        if( serviceProvider.ParseResponse( response , elevations ) )
                        {
                            //write entries to file:
                            foreach( float elevation in elevations )
                            {
                                writer.WriteLine( elevation );
                            }
                            elevations.Clear();
                        }
                        else
                        {
                            throw new System.Exception( "TODO: Act on api answer failed or denied" );
                        }
                    }

                    //
                    ticket.value = 1f - ( (float)coordinates.Count/(float)numStartingCoordinates );
                    repaintWindowCallback();

                    //test for abort:
                    if( ticket.invalid )
                    {
                        repaintWindowCallback();
                        throw new System.Exception( "aborted" );
                    }
                }

                //task done:
                ticket.value = 1f;
                repaintWindowCallback();
                Debug.Log( coordinates.Count==0 ? "SUCCESS!" : $"ERROR, unprocessed coordinates: { coordinates.Count } )" );
                await Task.CompletedTask;
            }
            catch ( System.Exception ex ) { Debug.LogException(ex); }
            finally
            {
                //log:
                Debug.Log( $"{ nameof(GetElevationData) }() finished" );

                //close streams:
                if( writer!=null ){ writer.Close(); }
                if( stream!=null ){ stream.Close(); }

                //call on finish:
                if( onFinish!=null ) { onFinish(); }
            }
        }

        /// Reads text file line by line and creates result file sinultanously
        public void ProcessTextFile ( string filePath , string filePathOutput , System.Func<string,string> process )
        {
            if( IO.File.Exists(filePath)==false )
            {
                Debug.LogError( $"file not found: { filePath }" );
                return;
            }

            IO.FileStream stream_reading = null;
            IO.FileStream stream_writing = null;
            IO.StreamReader reader = null;
            IO.StreamWriter writer = null;
            try
            {
                stream_reading = new IO.FileStream(
                    filePath ,
                    IO.FileMode.Open ,
                    IO.FileAccess.Read ,
                    IO.FileShare.Write ,
                    4096 ,
                    IO.FileOptions.SequentialScan
                );
                stream_writing = new IO.FileStream(
                    filePathOutput ,
                    IO.FileMode.Create ,
                    IO.FileAccess.Write ,
                    IO.FileShare.Read ,
                    4096 ,
                    IO.FileOptions.SequentialScan
                );
                reader = new IO.StreamReader( stream_reading );
                writer = new IO.StreamWriter( stream_writing );
                
                string line;
                while( (line = reader.ReadLine())!=null )
                {
                    if( line.Length!=0 )
                    {
                        string output = process( line );
                        writer.WriteLine( output );
                    }
                }
            }
            catch ( System.Exception ex ) { Debug.LogException(ex); }
            finally
            {
                if( stream_reading!=null ){ stream_reading.Close(); }
                if( reader!=null ){ reader.Close(); }
                if( writer!=null ){ writer.Close(); }
            }
        }

        public async void WriteImageFile (
            string filePath ,
            int width ,
            int height ,
            Vector2 clampElevation ,
            Vector2 lerpElevation ,
            System.Action onFinish = null
        )
        {
            //get file path from user:
            {
                if( filePath.Length==0 )
                {
                    Debug.Log( "Cancelled by user" );
                    return;
                }
                if( IO.File.Exists(filePath)==false )
                {
                    Debug.LogError( $"File not found: { filePath }" );
                    return;
                }
            }

            //prepare samples:
            Debug.Log( $"Creating image...\nsource: { filePath }" );
            int[] pixels;
            {
                pixels = new int[ height * width ];
                IO.FileStream stream = null;
                IO.StreamReader reader = null;

                await Task.Run( ()=> {

                    try
                    {
                        stream = new IO.FileStream(
                            filePath ,
                            IO.FileMode.Open ,
                            IO.FileAccess.Read ,
                            IO.FileShare.Read
                        );
                        reader = new IO.StreamReader( stream );

                        int index = 0;
                        
                        //
                        string line = null;
                        while( (line = reader.ReadLine())!=null )
                        {
                            if( line.Length!=0 )
                            {
                                //calc color value:
                                float elevation = float.Parse( line );
                                float clamped = Mathf.Clamp( elevation , clampElevation.x , clampElevation.y );
                                float inverselerped = Mathf.InverseLerp( lerpElevation.x , lerpElevation.y , clamped );
                                int color = (int)( inverselerped * ushort.MaxValue );
                                
                                // find pixel position
                                int X = (width-1) - index%width;
                                int Y = (height-1) - index/width;
                                
                                // set pixel color
                                pixels[ Y * width + X ] = color;

                                //step
                                index++;
                            }
                        }
                    }
                    catch ( System.Exception ex ) { Debug.LogException(ex); }
                    finally
                    {
                        if( stream!=null ){ stream.Close(); }
                        if( reader!=null ){ reader.Close(); }
                    }

                } );
            }

            //write bytes to file:
            {
                string pngFilePath = $"{ filePath.Replace(".csv","") } elevations({ clampElevation.x },{ clampElevation.y }).png";
                Debug.Log( $"\twriting to PNG file: { pngFilePath }" );
                await PNG.WriteGrayscaleAsync(
                    pixels:    pixels ,
                    width:      width ,
                    height:     height ,
                    bitDepth:   16 ,
                    alpha:      false ,
                    filePath:   pngFilePath
                );
                filePath = null;
                Debug.Log( $"\tPNG file ready: { pngFilePath }" );
            }

            //call on finish:
            if( onFinish!=null ) { onFinish(); }
        }

        public void ForEachLine ( string filePath , System.Action<string> action )
        {
            IO.FileStream stream_reading = null;
            IO.StreamReader reader = null;
            try
            {
                stream_reading = new IO.FileStream(
                    filePath ,
                    IO.FileMode.Open ,
                    IO.FileAccess.Read ,
                    IO.FileShare.Read ,
                    4096 ,
                    IO.FileOptions.SequentialScan
                );
                reader = new IO.StreamReader( stream_reading );
                
                string line;
                while( (line = reader.ReadLine())!=null )
                {
                    if( line.Length!=0 )
                    {
                        action( line );
                    }
                }
            }
            catch ( System.Exception ex ) { Debug.LogException(ex); }
            finally
            {
                if( stream_reading!=null ){ stream_reading.Close(); }
                if( reader!=null ){ reader.Close(); }
            }
        }   

        public double HaversineDistance ( Coordinate A , Coordinate B )
        {
            double DegreesToRadians ( double degrees ) { return (System.Math.PI / 180d) * degrees; }
            double R = 6371e3d;//metres
            double φ1 = DegreesToRadians( A.latitude );
            double φ2 = DegreesToRadians( B.latitude );
            double Δφ = DegreesToRadians( B.latitude - A.latitude );
            double Δλ = DegreesToRadians( B.longitude - A.longitude );
            double a = System.Math.Sin( Δφ / 2d ) * System.Math.Sin( Δφ / 2d ) +
                    System.Math.Cos( φ1 ) * System.Math.Cos( φ2 ) *
                    System.Math.Sin( Δλ / 2d ) * System.Math.Sin( Δλ / 2d );
            double c = 2d * System.Math.Atan2( System.Math.Sqrt( a ) , System.Math.Sqrt( 1d - a ) );
            double d = R * c;
            return d;
        }

        public string GetFolderPath ()
        {
            string folderPath = $"{ Application.dataPath }/../ElevationMapExporter/";
            if( IO.Directory.Exists( folderPath )==false ) { IO.Directory.CreateDirectory( folderPath ); }
            return folderPath;
        }

        public string GetFileNamePrefix
        (
            Coordinate start ,
            Coordinate end ,
            CoordinateInt resolution
        )
        {
            return $"region({ start.latitude },{ start.longitude })-({ end.latitude },{ end.longitude }) resolution({ resolution.latitude },{ resolution.longitude })";
        }

        #endregion
        #region interface implementation

        public void Dispose ()
        {
            _client.CancelPendingRequests();
            _client.Dispose();
        }

        #endregion
    }
}
