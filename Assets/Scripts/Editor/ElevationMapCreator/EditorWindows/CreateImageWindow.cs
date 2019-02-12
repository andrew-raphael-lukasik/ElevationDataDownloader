using System.Collections.Generic;
using System.Threading.Tasks;
//using System.Net.Http;

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
                    GUILayout.Label( "Clamp Elevation:" , GUILayout.Width(100f) );
                    _owner.createImageSettings.clampElevation.x = EditorGUILayout.FloatField( _owner.createImageSettings.clampElevation.x , GUILayout.Width(60f) );
                    GUILayout.Label( "-" , GUILayout.Width(10f) );
                    _owner.createImageSettings.clampElevation.y = EditorGUILayout.FloatField( _owner.createImageSettings.clampElevation.y , GUILayout.Width(60f) );

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
                if( GUILayout.Button( "Create Image" , GUILayout.Height(EditorGUIUtility.singleLineHeight*2f) ) )
                {
                    _owner.core.WriteImageFile(
                        _filePath ,
                        _owner.createImageSettings.resolution.longitude ,
                        _owner.createImageSettings.resolution.latitude ,
                        _owner.createImageSettings.clampElevation ,
                        EditorWindow.GetWindow<CreateImageWindow>().Show
                    );
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
                && System.IO.File.Exists( _filePath )==true
            )
            {
                System.IO.FileStream stream = null;
                System.IO.StreamReader reader = null;
                try
                {
                    stream = new System.IO.FileStream(
                        _filePath ,
                        System.IO.FileMode.Open ,
                        System.IO.FileAccess.Read ,
                        System.IO.FileShare.Read ,
                        4096 ,
                        System.IO.FileOptions.SequentialScan
                    );
                    reader = new System.IO.StreamReader( stream );

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
            public Vector2 clampElevation = new Vector2 ( -100f , 1900f );
        }

        #endregion
    }
}
