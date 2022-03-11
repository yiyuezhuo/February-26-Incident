namespace YYZ.App
{


using YYZ.Data.February26;
public class MapView : YYZ.MapKit.MapView<Region>
{
    public override void _Ready()
    {
        base._Ready();
        
        arrowContainer.Hide();
        mapImageContainer.Hide();
    }

}


}