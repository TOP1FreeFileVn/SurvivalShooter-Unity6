using UnityEngine;

// Âm thanh "ting" khi nhặt EXP, tạo bằng code vì bộ asset không có sẵn
public class PickupSound : MonoBehaviour
{
    static PickupSound instance;
    static AudioClip clip;

    AudioSource source;
    float lastPlay;

    public static void Play()
    {
        if (instance == null)
        {
            instance = new GameObject("PickupSound").AddComponent<PickupSound>();
            instance.source = instance.gameObject.AddComponent<AudioSource>();
            instance.source.volume = 0.25f;
            if (clip == null) clip = MakeClip();
        }
        // Giới hạn tần suất để không bị chói khi hút nhiều viên cùng lúc
        if (Time.unscaledTime - instance.lastPlay < 0.04f) return;
        instance.lastPlay = Time.unscaledTime;
        instance.source.pitch = Random.Range(0.95f, 1.15f);
        instance.source.PlayOneShot(clip);
    }

    static AudioClip MakeClip()
    {
        const int rate = 44100;
        int len = (int)(rate * 0.12f);
        var data = new float[len];
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / rate;
            float env = Mathf.Exp(-t * 35f);
            data[i] = env * (Mathf.Sin(2f * Mathf.PI * 1320f * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * 1980f * t) * 0.3f);
        }
        var c = AudioClip.Create("XpPickup", len, 1, rate, false);
        c.SetData(data, 0);
        return c;
    }
}
