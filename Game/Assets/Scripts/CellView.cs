using System.Collections;
using UnityEngine;

public class CellView : MonoBehaviour
{
    public int x, y;
    private SpriteRenderer sr;
    private GameOfLife controller;
    private Vector3 baseScale;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
    }

    public void Init(int gx, int gy, GameOfLife ctrl)
    {
        x = gx; y = gy; controller = ctrl;
    }

    public void SetState(bool alive, GameOfLife.Owner owner)
    {
        if (alive)
        {
            if (owner == GameOfLife.Owner.White) sr.color = Color.white;
            else if (owner == GameOfLife.Owner.Black) sr.color = Color.black;
            else sr.color = Color.white * 0.9f;
            sr.enabled = true;
        }
        else
        {
            sr.enabled = false;
        }
    }

    void OnMouseDown()
    {
        if (controller == null) return;
        controller.ToggleCellAt(x, y);
    }

    public void AnimateBirth()
    {
        StopAllCoroutines();
        StartCoroutine(ScalePulse(0.1f, 1.2f));
    }

    public void AnimateDeath()
    {
        StopAllCoroutines();
        StartCoroutine(ScalePulse(0.08f, 0.6f, true));
    }

    IEnumerator ScalePulse(float dur, float peak, bool shrink = false)
    {
        float t = 0f;
        Vector3 from = baseScale * (shrink ? 1f : 0.2f);
        Vector3 to = baseScale * peak;
        transform.localScale = from;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = t / dur;
            transform.localScale = Vector3.Lerp(from, to, p);
            yield return null;
        }
        transform.localScale = baseScale;
    }
}
