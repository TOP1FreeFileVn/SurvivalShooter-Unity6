using UnityEngine;
using UnityEngine.UI;

// Số sát thương bay lên tại điểm trúng đạn (hiển thị trên canvas overlay)
public class DamagePopup : MonoBehaviour
{
    public static RectTransform container;
    public static Font font;

    Vector3 worldPos;
    Text text;
    float life;
    const float Duration = 0.7f;

    public static void Show(Vector3 worldPos, int amount, bool crit)
    {
        if (container == null) return;
        var go = new GameObject("DamagePopup", typeof(RectTransform), typeof(Text), typeof(Outline));
        go.transform.SetParent(container, false);
        var p = go.AddComponent<DamagePopup>();
        p.worldPos = worldPos + new Vector3(Random.Range(-0.3f, 0.3f), 0.8f, 0f);
        p.text = go.GetComponent<Text>();
        p.text.font = font;
        p.text.fontSize = crit ? 40 : 28;
        p.text.text = crit ? amount + "!" : amount.ToString();
        p.text.color = crit ? new Color(1f, 0.35f, 0.2f) : Color.white;
        p.text.alignment = TextAnchor.MiddleCenter;
        p.text.horizontalOverflow = HorizontalWrapMode.Overflow;
        p.text.raycastTarget = false;
        go.GetComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);
        ((RectTransform)go.transform).sizeDelta = new Vector2(120, 50);
        p.UpdatePosition();
    }

    void Update()
    {
        life += Time.deltaTime;
        worldPos += Vector3.up * 1.5f * Time.deltaTime;
        var c = text.color;
        c.a = 1f - Mathf.Clamp01((life - Duration * 0.5f) / (Duration * 0.5f));
        text.color = c;
        UpdatePosition();
        if (life >= Duration) Destroy(gameObject);
    }

    void UpdatePosition()
    {
        var cam = Camera.main;
        if (cam == null) return;
        Vector3 sp = cam.WorldToScreenPoint(worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(container, sp, null, out Vector2 local);
        ((RectTransform)transform).anchoredPosition = local;
    }
}
