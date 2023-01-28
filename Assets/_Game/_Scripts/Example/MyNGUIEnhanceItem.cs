using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// NGUI Enhance item example
/// </summary>
public class MyNGUIEnhanceItem : EnhanceItem
{
    private Image mTexture;

    protected override void OnAwake()
    {
        this.mTexture = GetComponent<Image>();
        UIEventListener.Get(this.gameObject).onClick = OnClickNGUIItem;
    }

    private void OnClickNGUIItem(GameObject obj)
    {
        this.OnClickEnhanceItem();
    }

    // Set the item "depth" 2d or 3d
    protected override void SetItemDepth(float depthCurveValue, int depthFactor, float itemCount)
    {
        if (mTexture.depth != (int)Mathf.Abs(depthCurveValue * depthFactor))
            mTexture.depth = (int)Mathf.Abs(depthCurveValue * depthFactor);
    }

    // Item is centered
    public override void SetSelectState(bool isCenter)
    {
        if (mTexture == null)
            mTexture = this.GetComponent<Image>();
        if (mTexture != null)
            mTexture.color = isCenter ? Color.white : Color.gray;
    }

    protected override void OnClickEnhanceItem()
    {
        // item was clicked
        base.OnClickEnhanceItem();
    }
}
