using UnityEngine;
using UnityEngine.Splines;

namespace Unity.Factory.Sample
{
    public class CustomSplineAnimate : MonoBehaviour
    {
        [SerializeField]
        SplineContainer m_SplineContainer;

        [SerializeField]
        float m_Speed = 1f;

        float m_Time;

        void Update()
        {
            if (m_SplineContainer == null)
            {
                return;
            }

            var spline = m_SplineContainer.Spline;
            if (spline == null)
            {
                return;
            }

            var length = spline.GetLength();
            // Evita divisão por zero se o Spline for vazio ou tiver comprimento nulo
            if (length <= 0.001f)
            {
                return;
            }

            m_Time += Time.deltaTime * m_Speed;
            var t = m_Time / length;

            if (t > 1f)
            {
                m_Time = 0f;
                t = 0f;
            }

            // Converte a posição local/global do Spline para o transform
            Vector3 targetPos = spline.EvaluatePosition(t);
            
            // Evita definir posições inválidas (Infinity/NaN)
            if (!float.IsNaN(targetPos.x) && !float.IsInfinity(targetPos.x))
            {
                // Se o spline container estiver em coordenadas locais, precisamos converter para mundo
                // spline.EvaluatePosition retorna a posição local do spline.
                // Devemos transformar essa posição local para mundo usando o transform do spline container.
                transform.position = m_SplineContainer.transform.TransformPoint(targetPos);
            }
            
            // Avalia rotação e rotaciona o personagem para seguir a direção do caminho de forma suave
            Vector3 localTangent = spline.EvaluateTangent(t);
            Vector3 worldTangent = m_SplineContainer.transform.TransformDirection(localTangent);
            if (worldTangent != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(worldTangent);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 8f);
            }
        }
    }
}