using System.Collections;
using UnityEngine;

public class CameraOutroController : MonoBehaviour
{
    [SerializeField] private Transform targetPosition;
    [SerializeField] private float moveTime = 2f;

    public IEnumerator PlayOutro()
    {
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        float t = 0f;

        while (t < moveTime)
        {
            t += Time.deltaTime;

            float lerp = t / moveTime;

            transform.position = Vector3.Lerp(
                startPos,
                targetPosition.position,
                lerp);

            transform.rotation = Quaternion.Slerp(
                startRot,
                targetPosition.rotation,
                lerp);

            yield return null;
        }

        transform.position = targetPosition.position;
        transform.rotation = targetPosition.rotation;
    }
}