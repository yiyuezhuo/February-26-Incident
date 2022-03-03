namespace YYZ.MapKit
{


using System.Collections;
using System.Collections.Generic;
using Godot;
using System;

public interface IArea
{
    Color remapColor{get;}
}

public interface IMapData<TArea>
{
    int width{get;}
    int height{get;}
    TArea ColorToArea(Color color);
    Color? Pos2Color(Vector2 p);
    void EnsureSetup();
}

/// <summary>
/// Manage a complex consisting of `Image` and `ImageTexture`. Caller should gather "ChangeAreaColor" calls in a place, and call `Flush` to apply the changes.
/// If no change exists, `Flush` will not introduce overhead. So the "sentinel" variable is not necessary, you can just call `Flush`to ensure the change to get apply.
/// However, you should not add `Flush` below every `ChangeAreaColor`.
/// 
/// Basically, this class represents a "Palette" 
/// </summary>
public class ImageTextureStrap<TArea> where TArea : IArea
{
    Image image;
    ImageTexture texture;
    bool Locked = false;
    Dictionary<AreaInfo<TArea>, Color> cacheMap = new Dictionary<AreaInfo<TArea>, Color>(); // We use AreaInfo hash here, since remapColor hash is not efficient enough.

    public ImageTextureStrap(Image image, ImageTexture texture)
    {
        this.image = image;
        this.texture = texture;
    }

    void ChangeColor(Color remapColor, Color showColor)
    {
        int i = remapColor.r8;
        int j = remapColor.g8;
        image.SetPixel(i, j, showColor);
    }

    /// <summary>
    /// Change color area color
    /// </summary>
    public void ChangeAreaColor(AreaInfo<TArea> area, Color color)
    {
        // TODO: Benchmark whether this sentry improves performance.
        if(cacheMap.TryGetValue(area, out var prevColor))
        {
            // GD.Print($"area={area}, color={color}, prevColor={prevColor} => {color.Equals(prevColor)}");
            if(color.Equals(prevColor))
                return;
            cacheMap[area] = color;
        }
        else
        {
            cacheMap[area] = color;
        }
        

        if(!Locked)
        {
            BeginChangeColor();
        }
        ChangeColor(area.remapColor, color);
    }

    void BeginChangeColor()
    {
        // GD.Print("before Lock");
        image.Lock();
        Locked = true;
    }

    void EndChangeColor()
    {
        image.Unlock();
        texture.SetData(image); // TODO: improve performance
        Locked = false;
    }

    public void Flush()
    {
        if(Locked){
            EndChangeColor();
        }
    }
}


/// <summary>
/// Provides "high level" area displaying property (ex. foregroundColor, isMixMode) so "high level" controller don't need to know detail about remapColor.
/// The detailed palette mapping rule defined here may mutate in future.
/// While the `remapColor` is public, all interfaces it implemented are expected to not include this attribute/property.
///
/// Virtual DOM-like API, caller can set duplicated value will not make real change or image locking or updating.
/// </summary>
public class AreaInfo<TArea> where TArea : IArea
{
    readonly MapShower<TArea> mapShower; // TODO: lambda refactor?
    readonly public Color remapColor; // Or we can keep a reference to object or yet another interface.

    public AreaInfo(Color remapColor, MapShower<TArea> mapShower)
    {
        this.remapColor = remapColor;
        this.mapShower = mapShower;
    }

    public Color ToModeColor() // TODO: Do we need ToForegroundColor, ToBackgroundColor
    {
        return new Color(isSelecting ? 1.0f : 0.0f, isMixMode ? 1.0f : 0.0f, isHighlighting ? 1.0f : 0.0f, 0.0f);
    }

    public Color foregroundColor
    {
        set => mapShower.foregroundStrap.ChangeAreaColor(this, value);
    }
    public Color backgroundColor
    {
        set => mapShower.backgroundStrap.ChangeAreaColor(this, value);
    }
    
    bool _isSelecting;
    public bool isSelecting
    {
        get => _isSelecting;
        set
        {
            if(_isSelecting != value)
            {
                _isSelecting = value;
                mapShower.modeStrap.ChangeAreaColor(this, ToModeColor());
            }
        }
    }
    bool _isMixMode;
    public bool isMixMode
    {
        get => _isMixMode;
        set
        {
            if(_isMixMode != value)
            {
                _isMixMode = value;
                mapShower.modeStrap.ChangeAreaColor(this, ToModeColor());
            }
        }
    }

    bool _isHighlighting;// = false;
    public bool isHighlighting
    {
        get => _isHighlighting;
        set
        {
            if(_isHighlighting != value)
            {
                _isHighlighting = value;
                mapShower.modeStrap.ChangeAreaColor(this, ToModeColor());
            }
        }
    }
    
}


/// <summary>
/// Map sprite manager. MapShower itself will not capture any event, but it comes with some `OnXYZ` handlers to be called from outside and
/// invokes some events. While `Strap`s members are public, they should not be included in any implemented interfaces. 
/// We will just use those public members from limited horizon, such as the file defined in MapShower itself.
/// We may introduce namespace to enforce or hint this in future, but we will enforce interface rule only at this time.
/// </summary>
public class MapShower<TArea> : Sprite where  TArea : IArea
{
    [Export] Resource mapDataResource; // TODO: fallback to Mapdata?

    IMapData<TArea> mapData;

    int width{get => mapData.width;}
    int height{get => mapData.height;}

    // Other configs
    
    public event EventHandler<TArea> areaSelectedEvent;
    public event EventHandler<TArea> areaClickEvent;

    // States
    public ImageTextureStrap<TArea> foregroundStrap;
    public ImageTextureStrap<TArea> backgroundStrap;
    public ImageTextureStrap<TArea> modeStrap;

    TArea areaLastSelected = default(TArea);
    Dictionary<TArea, AreaInfo<TArea>> areaToAreaInfo = new Dictionary<TArea, AreaInfo<TArea>>();

    public override void _Ready()
    {
        // GD.Print($"mapDataResource={mapDataResource}, typeof(TArea)={typeof(TArea)}");
        mapData = (IMapData<TArea>)mapDataResource; // TODO: Casting performance problem?
        mapData.EnsureSetup(); // TODO: Looks like a very ugly implementation leak

        var material = (ShaderMaterial)this.Material;

        foregroundStrap = CreateStrap(new Color(1,1,1,1), material, "foreground_palette_texture");
        backgroundStrap = CreateStrap(new Color(0,1,0,1), material, "background_palette_texture");
        modeStrap = CreateStrap(new Color(0,0,0,0), material, "mode_palette_texture");
    }

    /// <summary>
    /// Get a proxy which controls area displaying property.
    /// </summary>
    public AreaInfo<TArea> GetAreaInfo(TArea area)
    {
        if(areaToAreaInfo.TryGetValue(area, out var areaInfo))
        {
            return areaInfo;
        }
        else
        {
            areaInfo = new AreaInfo<TArea>(area.remapColor, this);
            areaToAreaInfo[area] = areaInfo;
            return areaInfo;
        }
    }

    /// <summary>
    /// Create a strap (thus the image and texture as well) and bind the texture to material property.
    /// </summary>
    static ImageTextureStrap<TArea> CreateStrap(Image image, ShaderMaterial material, string paramName)
    {
        var texture = new ImageTexture();
        texture.CreateFromImage(image);

        // GD.Print($"texture.Flags={texture.Flags}");

        // Default is 7 -> (binary) 111 -> (FLAG_MIPMAPS, FLAG_REPEAT, FLAG_FILTER) = (true, true, true)
        texture.Flags = 0; // Disable those misleading "optimizations":
        //  0 -> (binary) 000 -> (binary) 111 -> (FLAG_MIPMAPS, FLAG_REPEAT, FLAG_FILTER) = (false, false, false)

        // GD.Print($"texture.Flags={texture.Flags}");

        material.SetShaderParam(paramName, texture);
        return new ImageTextureStrap<TArea>(image, texture);
    }
    static ImageTextureStrap<TArea> CreateStrap(Color colorInit, ShaderMaterial material, string paramName)
    {
        return CreateStrap(CreateInitImage(colorInit), material, paramName);
    }

    /*
    static ImageTextureStrap CreateStrap(ImageTextureStrap strap, ShaderMaterial material, string paramName)
    {
        return CreateStrap((Image)strap.image.Duplicate(), material, paramName);
    }
    */

    static Image CreateInitImage(Color colorInit)
    {
        /*
        const int arrLen = 256*256*4;

        byte[] poolByteArray = new byte[arrLen];

        var r = (byte)colorInit.r8;
        var g = (byte)colorInit.g8;
        var b = (byte)colorInit.b8;
        var a = (byte)colorInit.a8;

        for(int i=0; i<arrLen; i+=4)
        {
            
            poolByteArray[i] = r;
            poolByteArray[i+1] = g;
            poolByteArray[i+2] = b;
            poolByteArray[i+3] = a;
            
            //poolByteArray[i] = 0;
            //poolByteArray[i+1] = 0;
            //poolByteArray[i+2] = 0;
            //poolByteArray[i+3] = 0;
        }

        var paletteImage = new Image();
        paletteImage.CreateFromData(256, 256, false, Image.Format.Rgba8, poolByteArray);
        */

        
        var paletteImage = new Image();
        paletteImage.Create(256, 256, false, Image.Format.Rgba8);
        paletteImage.Fill(colorInit);

        paletteImage.Lock();
        // GD.Print($"paletteImage.GetPixel(0, 0)={paletteImage.GetPixel(0, 0)}");
        paletteImage.Unlock();

        return paletteImage;
    }

    /// <summary>
    /// If a Flush is not required, it will also not introduce extra overhead. 
    /// So we don't need to maintain sentinel (such as "needFlush") in caller code.
    /// </summary>
    public void Flush()
    {
        foregroundStrap.Flush();
        backgroundStrap.Flush();
        modeStrap.Flush();
    }

    /// <summary>
    /// Map hovering handler. It include fixed internal processing and "identifies" the `areaSelected` event which can be subscribed.
    /// </summary>
    public void OnRaycastHit(Vector2 p)
    {
        //GD.Print($"OnRaycstHit:{p}");
        
        var baseColorSelectedNullable = Pos2Color(p);
        //Debug.Log($"baseColorSelectedNullable={baseColorSelectedNullable}");
        if(baseColorSelectedNullable == null)
        {
            return;
        }
        var baseColorSelected = baseColorSelectedNullable.Value;
        // Debug.Log($"baseColorSelected={baseColorSelected}");
        var areaSelected = mapData.ColorToArea(baseColorSelected);
        //Debug.Log($"areaSelected={areaSelected}");

        // Debug.Log($"{areaLastSelected == null}, {areaSelected != areaLastSelected}, bc:{areaSelected.baseColor} rc:{areaSelected.remapColor}");
        // GD.Print($"areaSelected={areaSelected}, areaLastSelected={areaLastSelected}");
        if(!areaSelected.Equals(areaLastSelected))
        {
            // GD.Print($"Area select:{areaSelected}");
            if(areaLastSelected != null){
                GetAreaInfo(areaLastSelected).isSelecting = false;
            }

            GetAreaInfo(areaSelected).isSelecting = true;
            areaLastSelected = areaSelected;

            areaSelectedEvent?.Invoke(this, areaSelected); // Signal support only Godot.Object derived objects.

            Flush(); // modeStrap.Flush();
        }
    }
    
    public void OnClick(Vector2 pos)
    {
        // Abstract out following code duplication
        var baseColorSelectedNullable = Pos2Color(pos);
        if(baseColorSelectedNullable == null)
        {
            return;
        }
        var baseColorSelected = baseColorSelectedNullable.Value;
        var areaSelected = mapData.ColorToArea(baseColorSelected);

        areaClickEvent?.Invoke(this, areaSelected);
    }
    
    /*
    public void OnSwitchMode(int mode)
    {   var material = (ShaderMaterial)Material;
        material.SetShaderParam("_Mode", mode);
    }
    */

    public Color? Pos2Color(Vector2 p)
    {
        //Debug.Log("Pos2Color");
        return mapData.Pos2Color(p);
    }

    // public Vector2 MapToWorld(Vector2 mapPos) => mapData.MapToWorld(mapPos);
}


}