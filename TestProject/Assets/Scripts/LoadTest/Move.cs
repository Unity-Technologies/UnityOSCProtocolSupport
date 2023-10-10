using UnityEngine;

namespace LoadTest
{
    public class Move : MonoBehaviour
    {
        Vector3 m_InitialPosition;
        Vector3 m_Axis;

        void Start()
        {
            m_InitialPosition = transform.position;
            m_Axis = Random.onUnitSphere;
        }

        void Update()
        {
            transform.position = m_InitialPosition + Quaternion.Euler(0, 0, Time.time * 90f) * m_Axis;
        }
    }
}
