using System.Collections;
using UnityEngine;

public class CameraIntroController : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;

    [Header("Animation")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;

    [SerializeField]
    private float duration = 3f;

    [SerializeField]
    private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public IEnumerator PlayIntro()
    {
        if (cameraTransform == null ||
            startPoint == null ||
            endPoint == null)
            yield break;

        cameraTransform.SetPositionAndRotation(
            startPoint.position,
            startPoint.rotation);

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / duration);
            t = moveCurve.Evaluate(t);

            cameraTransform.position = Vector3.Lerp(
                startPoint.position,
                endPoint.position,
                t);

            cameraTransform.rotation = Quaternion.Slerp(
                startPoint.rotation,
                endPoint.rotation,
                t);

            yield return null;
        }

        cameraTransform.SetPositionAndRotation(
            endPoint.position,
            endPoint.rotation);
    }
}