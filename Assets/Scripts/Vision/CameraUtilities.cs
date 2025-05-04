using UnityEngine;
using UnityEngine.XR.MagicLeap;

public class CameraUtilities
{
    private static float _rayLength = 3;

    public static Vector3 CastRayFromScreenToWorldPoint(IntrinsicParameters icp, ExtrinsicParameters extrinsic, Vector2 screenPoint)
    {
        var width = icp.width;
        var height = icp.height;

        var viewportPoint = new Vector2(screenPoint.x / width, screenPoint.y / height);
        return CastRayFromViewPortToWorldPoint(icp, extrinsic, viewportPoint);
    }

    public static Vector3 CastRayFromViewPortToWorldPoint(IntrinsicParameters icp, ExtrinsicParameters extrinsic, Vector2 viewportPoint)
    {
        var undistortedViewportPoint = UndistortViewportPoint(icp, viewportPoint);

        Vector3 cameraPosition    = new Vector3(extrinsic.position[0], extrinsic.position[1], extrinsic.position[2]);
        Quaternion cameraRotation = new Quaternion(extrinsic.rotation[0], extrinsic.rotation[1], 
                                                  extrinsic.rotation[2], extrinsic.rotation[3]);

        Ray ray = RayFromViewportPoint(icp, undistortedViewportPoint, cameraPosition, cameraRotation);
        Vector3 hitPoint = ray.GetPoint(_rayLength);

        Debug.DrawRay(ray.origin, ray.direction * 3, Color.red, 2f);

        if (Physics.Raycast(ray, out RaycastHit hit, 100))
        {
            Debug.Log("Didn't hit");
            hitPoint   = hit.point;
            _rayLength = hit.distance;
        }

        return hitPoint;
    }

    public static Vector2 UndistortViewportPoint(IntrinsicParameters icp, Vector2 distortedViewportPoint)
    {
        var normalizedToPixel = new Vector2(icp.width / 2, icp.height / 2).magnitude;
        var pixelToNormalized = Mathf.Approximately(normalizedToPixel, 0) ? float.MaxValue : 1 / normalizedToPixel;
        var viewportToNormalized = new Vector2(icp.width * pixelToNormalized, icp.height * pixelToNormalized);
        
        var principalPointVector     = new Vector2(icp.principalPoint[0], icp.principalPoint[1]);
        var normalizedPrincipalPoint = principalPointVector * pixelToNormalized;
        var normalizedToViewport     = new Vector2(1 / viewportToNormalized.x, 1 / viewportToNormalized.y);

        Vector2 d = Vector2.Scale(distortedViewportPoint, viewportToNormalized);
        Vector2 o = d - normalizedPrincipalPoint;

        float K1 = (float)icp.distortion[0];
        float K2 = (float)icp.distortion[1];
        float P1 = (float)icp.distortion[2];
        float P2 = (float)icp.distortion[3];
        float K3 = (float)icp.distortion[4];

        float r2 = o.sqrMagnitude;
        float r4 = r2 * r2;
        float r6 = r2 * r4;

        float radial = K1 * r2 + K2 * r4 + K3 * r6;
        Vector3 u = d + o * radial;

        if (!Mathf.Approximately(P1, 0) || !Mathf.Approximately(P2, 0))
        {
            u.x += P1 * (r2 + 2 * o.x * o.x) + 2 * P2 * o.x * o.y;
            u.y += P2 * (r2 + 2 * o.y * o.y) + 2 * P1 * o.x * o.y;
        }

        return Vector2.Scale(u, normalizedToViewport);
    }
    
    public static Ray RayFromViewportPoint(MLCamera.IntrinsicCalibrationParameters icp, Vector2 viewportPoint, Vector3 cameraPos, Quaternion cameraRotation)
    {
        var width  = icp.Width;
        var height = icp.Height;
        var principalPoint = icp.PrincipalPoint;
        var focalLength = icp.FocalLength;

        Vector2 pixelPoint = new Vector2(viewportPoint.x * width, viewportPoint.y * height);
        Vector2 offsetPoint = new Vector2(pixelPoint.x - principalPoint.x, pixelPoint.y - (height - principalPoint.y));
        Vector2 unitFocalLength = new Vector2(offsetPoint.x / focalLength.x, offsetPoint.y / focalLength.y);

        Vector3 rayDirection = cameraRotation * new Vector3(unitFocalLength.x, unitFocalLength.y, 1).normalized;

        return new Ray(cameraPos, rayDirection);
    }

    public static Ray RayFromViewportPoint(IntrinsicParameters icp, Vector2 viewportPoint, Vector3 cameraPos, Quaternion cameraRotation)
    {
        var width = icp.width;
        var height = icp.height;        
        var principalPoint = new Vector2(icp.principalPoint[0], icp.principalPoint[1]);
        var focalLength = new Vector2(icp.focalLength[0], icp.focalLength[1]);

        Vector2 pixelPoint = new Vector2(viewportPoint.x * width, viewportPoint.y * height);
        Vector2 offsetPoint = new Vector2(pixelPoint.x - principalPoint.x, pixelPoint.y - (height - principalPoint.y));
        Vector2 unitFocalLength = new Vector2(offsetPoint.x / focalLength.x, offsetPoint.y / focalLength.y);

        Vector3 rayDirection = cameraRotation * new Vector3(unitFocalLength.x, unitFocalLength.y, 1).normalized;

        return new Ray(cameraPos, rayDirection);
    }

}