using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class ButtonScript : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    public bool useAnimationClick = false;
    [System.Serializable]
    public class MyOwnEvent : UnityEvent { }
    [SerializeField]
    private MyOwnEvent myOwnEvent = new MyOwnEvent();
    public MyOwnEvent onMyOwnEvent { get { return myOwnEvent; } set { myOwnEvent = value; } }

    private float currentScale = 1f, moveTime = 1f;
    private Vector3 startPosition, endPosition;

    private void Awake()
    {
        currentScale = transform.localScale.x;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (useAnimationClick)
        {
            transform.localScale = Vector3.one * (currentScale - (currentScale * 0.1f));
        }
    }

    public void OnPointerUp(PointerEventData pointerEventData)
    {
        if (useAnimationClick)
        {
            transform.localScale = Vector3.one * currentScale;
        }
    }

    public void OnPointerClick(PointerEventData pointerEventData)
    {
        MusicController.USE.PlaySound(MusicController.USE.clickSound);
        onMyOwnEvent.Invoke();
    }

    private IEnumerator TranslateToEndPos()
    {
        yield return TranslationToEndPos(transform, transform.localPosition, endPosition, moveTime);
    }

    private IEnumerator TranslationToEndPos(Transform thisTransform, Vector3 startPos, Vector3 endPos, float value)
    {
        float rate = 1.0f / value;
        float t = 0.0f;
        while (t < 1.0)
        {
            t += Time.deltaTime * rate;
            thisTransform.localPosition = Vector3.Lerp(startPos, endPos, Mathf.SmoothStep(0.0f, 1.0f, t));
            yield return null;
        }

        thisTransform.localPosition = endPosition;
    }

    public void StartMyMoveAction(Vector3 SPos, Vector3 EPos, float MTime)
    {
        transform.localPosition = SPos;
        startPosition = SPos;
        endPosition = EPos;

        moveTime = MTime;

        StartCoroutine(TranslateToEndPos());
    }
}