using UnityEngine;
using UnityEngine.UI;

public class BarSegment : MonoBehaviour
{
    public float SegmentHealth { 
        get
        {
            return layoutElement.flexibleWidth;
        } 
        set { layoutElement.flexibleWidth = value; }
    }
    public Image bar;
    public Image DPSBar;
    public LayoutElement layoutElement;
}