using System.Collections.Generic;
using System.Threading.Tasks;
//using System.Net.Http;
using IO = System.IO;

using UnityEngine;
using UnityEditor;

namespace ElevationMapCreator
{
    public class CreateImageWindow : EditorWindow
    {
        #region FIELDS
        

        MainWindow _owner = null;

        [System.NonSerialized] string _filePath = null;
        int _numDataPoints;
        ElevationRange _elevationRange = new ElevationRange();


        #endregion
        #region EDITOR WINDOW

        void OnGUI ()
        {
            //assertions:
            if( _owner==null ) { Close(); }

            //draw gui:
            GUILayout.BeginVertical( "Create Image" , "window" );
            {
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Label( "CSV File:" , GUILayout.Width(60f) );
                    //GUILayout.Label( _filePath );
                    if( GUILayout.Button( _filePath!=null ? _filePath : "none" ) )
                    {
                        //select file:
                        string newFilePath = EditorUtility.OpenFilePanel(
                            "open data file" ,
                            _owner.core.GetFolderPath() ,
                            "csv"
                        );

                        if( newFilePath!=null && newFilePath.Length!=0 )
                        {
                            //update file path:
                            _filePath = newFilePath;

                            //get elevation range:
                            ReadElevationRangeFromFile();
                        }
                        else
                        {
                            Debug.Log( "Cancelled by user" );
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Label( "Elevation Range:" , GUILayout.Width(100f) );
                    EditorGUILayout.FloatField( _elevationRange.min , GUILayout.Width(60f) );
                    GUILayout.Label( "-" , GUILayout.Width(10f) );
                    EditorGUILayout.FloatField( _elevationRange.max , GUILayout.Width(60f) );
                }
                EditorGUILayout.EndHorizontal();
                

                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Label( "Data Points:" , GUILayout.Width(120f) );
                    GUILayout.Label( _numDataPoints.ToString() , GUILayout.Width(100f) );
                }
                EditorGUILayout.EndHorizontal();


                GUILayout.FlexibleSpace();

                
                int requiredDataPoints = _owner.createImageSettings.resolution.latitude * _owner.createImageSettings.resolution.longitude;
                EditorGUILayout.BeginHorizontal();
                {
                    //
                    GUILayout.Label( "Offset Elevation:" , GUILayout.Width(100f) );
                    _owner.createImageSettings.offset = EditorGUILayout.FloatField( _owner.createImageSettings.offset , GUILayout.Width(60f) );

                    //
                    GUILayout.Label( "Map Elevation:" , GUILayout.Width(100f) );
                    _owner.createImageSettings.lerp.x = EditorGUILayout.FloatField( _owner.createImageSettings.lerp.x , GUILayout.Width(60f) );
                    GUILayout.Label( "-" , GUILayout.Width(10f) );
                    _owner.createImageSettings.lerp.y = EditorGUILayout.FloatField( _owner.createImageSettings.lerp.y , GUILayout.Width(60f) );

                    GUILayout.Space( 20 );
                    
                    //
                    GUILayout.Label( "Resolution:" , GUILayout.Width(70f) );
                    _owner.createImageSettings.resolution.latitude = Mathf.Clamp(
                        EditorGUILayout.IntField( _owner.createImageSettings.resolution.latitude , GUILayout.Width(60f) ) ,
                        1 ,
                        int.MaxValue
                    );
                    _owner.createImageSettings.resolution.longitude = Mathf.Clamp(
                        EditorGUILayout.IntField( _owner.createImageSettings.resolution.longitude , GUILayout.Width(60f) ) ,
                        1 ,
                        int.MaxValue
                    );
                    GUILayout.Label( $" = { requiredDataPoints } data points required" );
                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.EndHorizontal();

                if( _filePath!=null )
                {
                    int difference = _numDataPoints - requiredDataPoints;
                    if( difference!=0 )
                    {
                        EditorGUILayout.HelpBox( "RESOLUTION DOES NOT MATCH SELECTED CSV FILE\ntwo resolution values when multiplied must be equal to number of data points in this csv file (one data point for every pixel).\nHence resolution numbers are wrong and/or file is incomplete" , MessageType.Warning );
                        EditorGUILayout.HelpBox( $"TIP: CSV { (difference<0 ? "lacks" : "exceeds by") } { Mathf.Abs(difference) } entries" , MessageType.Info );
                    }
                }

                GUI.enabled = _filePath!=null;
                if( GUILayout.Button( "Create Image (this file only)" , GUILayout.Height(EditorGUIUtility.singleLineHeight*2f) ) )
                {
                    _owner.core.WriteImageFile(
                        _filePath ,
                        _owner.createImageSettings.resolution.longitude ,
                        _owner.createImageSettings.resolution.latitude ,
                        _owner.createImageSettings.offset ,
                        _owner.createImageSettings.lerp ,
                        EditorWindow.GetWindow<CreateImageWindow>().Show
                    );
                }
                if( GUILayout.Button( "Create Images (every file in folder)" , GUILayout.Height(EditorGUIUtility.singleLineHeight*2f) ) )
                {
                    string[] csvFiles = IO.Directory.GetFiles( IO.Path.GetDirectoryName( _filePath ) , "*.csv" );
                    foreach( var csv in csvFiles )
                    {
                        _owner.core.WriteImageFile(
                            csv ,
                            _owner.createImageSettings.resolution.longitude ,
                            _owner.createImageSettings.resolution.latitude ,
                            _owner.createImageSettings.offset ,
                            _owner.createImageSettings.lerp ,
                            EditorWindow.GetWindow<CreateImageWindow>().Show
                        );
                    }
                }
                GUI.enabled = true;
            }
            EditorGUILayout.EndVertical();
        }

        #endregion
        #region PRIVATE METHODS

        void ReadElevationRangeFromFile ()
        {
            //reset:
            _elevationRange.Reset();
            _numDataPoints = 0;

            //read range:
            if(
                _filePath!=null
                && IO.File.Exists( _filePath )==true
            )
            {
                IO.FileStream stream = null;
                IO.StreamReader reader = null;
                try
                {
                    stream = new IO.FileStream(
                        _filePath ,
                        IO.FileMode.Open ,
                        IO.FileAccess.Read ,
                        IO.FileShare.Read ,
                        4096 ,
                        IO.FileOptions.SequentialScan
                    );
                    reader = new IO.StreamReader( stream );

                    string line = null;
                    while( (line = reader.ReadLine())!=null )
                    {
                        float elevation = float.Parse( line );
                        _elevationRange.Append( elevation );
                        _numDataPoints++;
                    }
                }
                catch ( System.Exception ex ) { Debug.LogException(ex); }
                finally
                {
                    if( stream!=null ){ stream.Close(); }
                    if( reader!=null ){ reader.Close(); }
                }
            }
        }
        
        public static CreateImageWindow CreateWindow ( MainWindow owner )
        {
            var window = EditorWindow.GetWindow<CreateImageWindow>();
            window._owner = owner;
            //Vector2 size = new Vector2( 600f , 200f );
            //window.minSize = size;
            //window.maxSize = size;
            window.Show();
            return window;
        }

        #endregion
        #region NESTED TYPES
        
        [System.Serializable]
        public class Settings
        {
            public CoordinateInt resolution = new CoordinateInt{ latitude = 64 , longitude = 64 };
            public float offset = 0f;
            public Vector2 lerp = new Vector2Int{ x = 0 , y = ushort.MaxValue };
        }

        #endregion
    }
}
