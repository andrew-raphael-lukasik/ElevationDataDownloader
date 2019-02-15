using System.Collections.Generic;
using System.Threading.Tasks;
//using System.Net.Http;

using UnityEngine;
using UnityEditor;

namespace ElevationMapCreator
{
    public class MainWindow : EditorWindow
    {
        #region FIELDS

        
        #region user settings
        [SerializeField] MainWindow.Settings _settings = new MainWindow.Settings();
        public MainWindow.Settings settings { get{ return _settings; } }

        [SerializeField] CreateImageWindow.Settings _createImageSettings = new CreateImageWindow.Settings();
        public CreateImageWindow.Settings createImageSettings { get{ return _createImageSettings; } }

        [SerializeField] bool _logTraffic = true;
        [SerializeField] EOnFinished _onFinished = EOnFinished.doNothing;
        #endregion
        
        #region window runtime data
        Task _mainTask;
        Ticket<float> _taskTicket;
        bool _isReady;
        Vector2 _scroll_window;
        #endregion
        
        #region runtime program data
        IElevationServiceProvider _serviveProvider = new ElevationService_OpenElevation();//ElevationService_Google();
        
        ElevationMapCreatorCore _core;
        public ElevationMapCreatorCore core { get{ return _core; } }
        
        #endregion


        #endregion
        #region EDITOR WINDOW
        
        void OnGUI ()
        {
            _isReady = _mainTask==null || _mainTask.IsCompleted;
            GUI.enabled = _isReady;
            
            EditorGUILayout.Separator();
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label( "Service Provider Selected: " );
                GUILayout.Label( _serviveProvider!=null ? _serviveProvider.GetType().FullName : "NONE" );
            }
            EditorGUILayout.EndHorizontal();
            _scroll_window = EditorGUILayout.BeginScrollView( _scroll_window );
            {


                GUILayout.BeginVertical( "- SETTINGS -" , "window" );
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        GUILayout.Label( "REGION BOTTOM LEFT CORNER:" );

                        //GUILayout.FlexibleSpace();

                        GUILayout.Label( "latitude:" , GUILayout.Width(60f) );
                        _settings.start.latitude = Mathf.Clamp(
                            EditorGUILayout.FloatField( _settings.start.latitude , GUILayout.Width(60f) ) ,
                            -90f ,
                            90f
                        );
                        
                        GUILayout.Label( "longitude:" , GUILayout.Width(60f) );
                        _settings.start.longitude = Mathf.Clamp(
                            EditorGUILayout.FloatField( _settings.start.longitude , GUILayout.Width(60f) ) ,
                            -180f ,
                            180f
                        );
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        GUILayout.Label( "REGION UPPER RIGHT CORNER:" );

                        //GUILayout.FlexibleSpace();

                        GUILayout.Label( "latitude:" , GUILayout.Width(60f) );
                        _settings.end.latitude = Mathf.Clamp(
                            EditorGUILayout.FloatField( _settings.end.latitude , GUILayout.Width(60f) ) ,
                            -90f ,
                            90f
                        );

                        GUILayout.Label( "longitude:" , GUILayout.Width(60f) );
                        _settings.end.longitude = Mathf.Clamp(
                            EditorGUILayout.FloatField( _settings.end.longitude , GUILayout.Width(60f) ) ,
                            -180f ,
                            180f
                        );
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        GUILayout.FlexibleSpace();
                        GUILayout.Label( "(move region)" );
                        float w = _settings.end.longitude - _settings.start.longitude;
                        float h = _settings.end.latitude - _settings.start.latitude;
                        if( GUILayout.Button( "<" , GUILayout.Width(30f) ) )
                        {
                            _settings.start.longitude -= w;
                            _settings.end.longitude -= w;
                        }
                        if( GUILayout.Button( "^" , GUILayout.Width(30f) ) )
                        {
                            _settings.start.latitude += h;
                            _settings.end.latitude += h;
                        }
                        if( GUILayout.Button( "v" , GUILayout.Width(30f) ) )
                        {
                            _settings.start.latitude -= h;
                            _settings.end.latitude -= h;
                        }
                        if( GUILayout.Button( ">" , GUILayout.Width(30f) ) )
                        {
                            _settings.start.longitude += w;
                            _settings.end.longitude += w;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.PrefixLabel( "Resolution:" );

                        //GUILayout.FlexibleSpace();

                        //calculate meters per degree:
                        double metersPerDegreeLatitude;
                        double metersPerDegreeLongitude;
                        {
                            Coordinate middlePoint = _settings.start + ( ( _settings.end - _settings.start ) * 0.5f );
                            metersPerDegreeLatitude = _core.HaversineDistance(
                                new Coordinate {
                                    latitude = _settings.start.latitude ,
                                    longitude = middlePoint.longitude
                                } ,
                                new Coordinate {
                                    latitude = _settings.end.latitude ,
                                    longitude = middlePoint.longitude
                                }
                            ) / _settings.resolution.latitude;
                            metersPerDegreeLongitude = _core.HaversineDistance(
                                new Coordinate {
                                    latitude = middlePoint.latitude ,
                                    longitude = _settings.start.longitude
                                } ,
                                new Coordinate {
                                    latitude = middlePoint.latitude ,
                                    longitude = _settings.end.longitude
                                }
                            ) / _settings.resolution.longitude;
                        }

                        GUILayout.Label( "latitude:" , GUILayout.Width(60f) );
                        _settings.resolution.latitude = Mathf.Clamp(
                            EditorGUILayout.IntField( _settings.resolution.latitude , GUILayout.Width(60f) ) ,
                            1 ,
                            int.MaxValue
                        );
                        GUILayout.Label( $"({ metersPerDegreeLatitude.ToString("0.##") } [m])" );

                        GUILayout.Label( "longitude:" , GUILayout.Width(60f) );
                        _settings.resolution.longitude = Mathf.Clamp(
                            EditorGUILayout.IntField( _settings.resolution.longitude , GUILayout.Width(60f) ) ,
                            1 ,
                            int.MaxValue
                        );
                        GUILayout.Label( $"({ metersPerDegreeLongitude.ToString("0.##") } [m])" );
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    {
                        GUILayout.Label( "Max Coordinates Per Request" );
                        settings.maxCoordinatesPerRequest = EditorGUILayout.IntField( settings.maxCoordinatesPerRequest );
                    }
                    EditorGUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();

                

                GUILayout.FlexibleSpace();



                GUILayout.BeginVertical( "- PROCESS -" , "window" );
                {
                    //start button:
                    if( GUILayout.Button( "START HTTP REQUESTS" , GUILayout.Height( EditorGUIUtility.singleLineHeight*2f ) ) )
                    {
                        string filePath = EditorUtility.SaveFilePanel(
                            $"Save data file" ,
                            _core.GetFolderPath() ,
                            _core.GetFileNamePrefix(
                                _settings.start ,
                                _settings.end ,
                                _settings.resolution
                            ) ,
                            "csv"
                        );
                        if( filePath.Length!=0 )
                        {
                            _taskTicket = new Ticket<float>( 0f );
                            _mainTask = _core.GetElevationData(
                                filePath ,
                                _serviveProvider ,
                                _taskTicket ,
                                _settings.start ,
                                _settings.end ,
                                _settings.resolution ,
                                _settings.maxCoordinatesPerRequest ,
                                this.Repaint ,
                                ()=>
                                {
                                    if( _onFinished==EOnFinished.createImage )
                                    {
                                        _core.WriteImageFile(
                                            filePath.Replace( ".csv" , ".png" ) ,
                                            _settings.resolution.longitude ,
                                            _settings.resolution.latitude ,
                                            _createImageSettings.clamp ,
                                            _createImageSettings.lerp
                                        );
                                    }

                                    //flash editor window (will it even does that?)
                                    EditorWindow.GetWindow<MainWindow>().Show();
                                } ,
                                _logTraffic
                            );
                        }
                        else
                        {
                            Debug.Log( "Cancelled by user" );
                        }
                    }

                    bool working = _mainTask!=null && _mainTask.Status!=TaskStatus.RanToCompletion && _mainTask.Status!=TaskStatus.Canceled;

                    //progress bar:
                    if( working )
                    {
                        bool b = GUI.enabled;
                        GUI.enabled = true;
                        EditorGUI.ProgressBar( EditorGUILayout.GetControlRect() , _taskTicket.value , "progress" );
                        GUI.enabled = b;
                    }
                    
                    //abort button:
                    bool abortingInProgress = working && _taskTicket.invalid;
                    GUI.enabled = _isReady==false && abortingInProgress==false;
                    {
                        string abortButtonLabel = abortingInProgress ? "Aborting..." : "Abort";
                        if( GUILayout.Button( abortButtonLabel ) )
                        {
                            _taskTicket.Invalidate();
                        }
                    }
                    GUI.enabled = _isReady;
                    
                    //toggles
                    {
                        bool GUIenabled = GUI.enabled;
                        GUI.enabled = true;
                        {
                            //log traffic toggle:
                            _logTraffic = EditorGUILayout.Toggle( "log traffic:" , _logTraffic );

                            //do on finished:
                            _onFinished = (EOnFinished)EditorGUILayout.EnumFlagsField( "On Finished:" , _onFinished );
                        }
                        GUI.enabled = GUIenabled;
                    }
                }
                GUILayout.EndVertical();



                GUILayout.FlexibleSpace();


                
                GUILayout.BeginVertical( "- TOOLS -" , "window" );
                {
                    //tools are (should be) independent from http process, so make sure GUI is enabled: 
                    GUI.enabled = true;

                    //
                    if( GUILayout.Button( "Create Image" , GUILayout.Height(EditorGUIUtility.singleLineHeight*2f) ) )
                    {
                        CreateImageWindow.CreateWindow( this );
                    }
                }
                GUILayout.EndVertical();
                EditorGUILayout.Separator();

                
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Separator();
        }

        void OnEnable ()
        {
            _core = new ElevationMapCreatorCore();
        }

        void OnDisable ()
        {
            _core.Dispose();
        }

        #endregion
        #region PRIVATE METHODS

        [MenuItem( "Tools/Elevation Map Exporter" )]
        public static MainWindow CreateWindow ()
        {
            var window = EditorWindow.GetWindow<MainWindow>();
            //Vector2 size = new Vector2( 600f , 40f );
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
            public Coordinate start = new Coordinate{ latitude = -90f , longitude = -180f };
            public Coordinate end = new Coordinate{ latitude = 90f , longitude = 180f };
            public CoordinateInt resolution = new CoordinateInt{ latitude = 64 , longitude = 64 };
            public int maxCoordinatesPerRequest = 2200;
        }

        public enum EOnFinished { doNothing , createImage }

        #endregion
    }
}
