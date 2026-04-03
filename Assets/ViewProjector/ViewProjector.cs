using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ZYFTemplate
{
    /// <summary>
    /// 请在此添加脚本描述
    /// </summary>
	public class ViewProjector : MonoBehaviour
    {
        public Texture previewTexture;

        public float horizontalFOV = 60f;

        public float verticalFOV = 45f;

        float nearClipDistance = 0.01f;

        public float projectDistance = 10f;

        public float sphereBlendAlpha = 0f;

        public float depthBias = 0.02f;

        public float gamma = 2.2f;

        public float brightness = 1f;

        //public bool useFarPlane = false;
      

        public int depthSize = 1024;

        //贴花对象引用
        DecalProjector decal;

        public Shader projectShader;

        //材质对象引用
        Material decalMaterial;

        RenderTexture depthTexture;

        Camera depthCamera;



        void Start()
        {
            UpdateProjection();
        }

        private void OnDestroy()
        {
            depthCamera.targetTexture = null;
            depthTexture?.Release();
        }

        void OnValidate()
        {
            Debug.Log("重新构建对象");
            
            UpdateProjection();
        }

        public void SetProjectedTexture(Texture texture)
        {
            decalMaterial.SetTexture("_Image", texture);
        }

        public void UpdateProjection()
        {
            if (decal == null)
            {
                decal = GetComponentInChildren<DecalProjector>();

            }

            if (decalMaterial == null)
            {
                if (projectShader == null)
                {
                    Debug.Log("投影着色器不存在");
                    return;
                }
                decalMaterial = new Material(projectShader);
            }

            decal.material = decalMaterial;
            decalMaterial.SetTexture("_Image", previewTexture);
            decalMaterial.SetFloat("_Gamma", gamma);
            decalMaterial.SetFloat("_Brightness", brightness);
            decalMaterial.SetFloat("_SphereBlendAlpha", sphereBlendAlpha);           

            if (depthTexture == null)
            {
                depthTexture = new RenderTexture(depthSize, depthSize, 24);
                depthTexture.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.None;
                depthTexture.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D32_SFloat_S8_UInt;
            }

            float tanHalfH = Mathf.Tan(horizontalFOV * 0.5f * Mathf.Deg2Rad);
            float tanHalfV = Mathf.Tan(verticalFOV * 0.5f * Mathf.Deg2Rad);
            int depthSizeY = (int)(depthSize * tanHalfV / tanHalfH);
            if (depthSize > 0 && (depthTexture.width != depthSize || depthTexture.height != depthSizeY))
            {
                depthTexture.Release();
                depthTexture.width = depthSize;
                depthTexture.height = depthSizeY;
            }

            decalMaterial.SetTexture("_Depth", depthTexture);
            decalMaterial.SetFloat("_Near", nearClipDistance);
            decalMaterial.SetFloat("_Far", projectDistance);
            decalMaterial.SetFloat("_DepthBias", depthBias);

            if (depthCamera == null)
            {
                depthCamera = GetComponentInChildren<Camera>();
            }

            depthCamera.nearClipPlane = nearClipDistance;
            depthCamera.farClipPlane = projectDistance;
            depthCamera.fieldOfView = verticalFOV;               
            depthCamera.targetTexture = depthTexture;
            depthCamera.ResetAspect();

            Vector3 scale = Vector3.one;
            scale.z = projectDistance;
            scale.x = projectDistance * Mathf.Tan(horizontalFOV * Mathf.Deg2Rad * 0.5f) * 2;
            scale.y = projectDistance * Mathf.Tan(verticalFOV * Mathf.Deg2Rad * 0.5f) * 2;
            decal.transform.localScale = scale;
        }

        public void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            float extentX = projectDistance * Mathf.Tan(horizontalFOV * Mathf.Deg2Rad * 0.5f);
            float extentY = projectDistance * Mathf.Tan(verticalFOV * Mathf.Deg2Rad * 0.5f);
            Gizmos.DrawLine(Vector3.zero, new Vector3(-extentX, extentY, projectDistance));
            Gizmos.DrawLine(Vector3.zero, new Vector3(extentX, extentY, projectDistance));
            Gizmos.DrawLine(Vector3.zero, new Vector3(extentX, -extentY, projectDistance));
            Gizmos.DrawLine(Vector3.zero, new Vector3(-extentX, -extentY, projectDistance));
            Gizmos.DrawLine(new Vector3(-extentX, extentY, projectDistance), new Vector3(extentX, extentY, projectDistance));
            Gizmos.DrawLine(new Vector3(extentX, extentY, projectDistance), new Vector3(extentX, -extentY, projectDistance));
            Gizmos.DrawLine(new Vector3(extentX, -extentY, projectDistance), new Vector3(-extentX, -extentY, projectDistance));
            Gizmos.DrawLine(new Vector3(-extentX, -extentY, projectDistance), new Vector3(-extentX, extentY, projectDistance));

            Gizmos.color = Color.red;
            Gizmos.DrawLine(Vector3.zero, new Vector3(0, 0, projectDistance));
        }
    }
}

