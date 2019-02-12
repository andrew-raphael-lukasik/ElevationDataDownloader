# Elevation data downloader
Project goal: Enable download of specific elevation data ranges and create lossless heightmap files

#
Features:
- Raw elevation data is stored in csv accumulator files
- CSV can be converted to 1 channel * 16bit PNG (you can convert those to RAW by yourself if needed)
- Program uses HTTP GET/POST methods to communicate. POST is default one.
- You can use https://github.com/andrew-raphael-lukasik/pngcs to import those 16bit png as Texture2D
- Implemented data providers:

    - api.open-elevation.com/api/ (default)
    
    - maps.googleapis.com/maps/api/elevation/ (requires key)
    
- IElevationServiceProvider makes adding new data providers easy 
#
