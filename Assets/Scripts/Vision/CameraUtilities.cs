using UnityEngine;
using UnityEngine.XR.MagicLeap;

public class CameraUtilities
{
    private static float _rayLength = 3;

    public static Vector3 CastRayFromScreenToWorldPoint(MLCamera.IntrinsicCalibrationParameters icp, Matrix4x4 cameraTransformMatrix, Vector2 screenPoint)
    {
        var width = icp.Width;
        var height = icp.Height;

        var viewportPoint = new Vector2(screenPoint.x / width, screenPoint.y / height);
        return CastRayFromViewPortToWorldPoint(icp, cameraTransformMatrix, viewportPoint);
    }

    public static Vector3 CastRayFromViewPortToWorldPoint(MLCamera.IntrinsicCalibrationParameters icp, Matrix4x4 cameraTransformMatrix, Vector2 viewportPoint)
    {
        var undistortedViewportPoint = UndistortViewportPoint(icp, viewportPoint);

        Ray ray = RayFromViewportPoint(icp, undistortedViewportPoint, cameraTransformMatrix.GetPosition(), cameraTransformMatrix.rotation);
        Vector3 hitPoint = ray.GetPoint(_rayLength);

        if (Physics.Raycast(ray, out RaycastHit hit, 100))
        {
            hitPoint = hit.point;
            _rayLength = hit.distance;
        }

        return hitPoint;
    }

    public static Vector2 UndistortViewportPoint(MLCamera.IntrinsicCalibrationParameters icp, Vector2 distortedViewportPoint)
    {
        var normalizedToPixel = new Vector2(icp.Width / 2, icp.Height / 2).magnitude;
        var pixelToNormalized = Mathf.Approximately(normalizedToPixel, 0) ? float.MaxValue : 1 / normalizedToPixel;
        var viewportToNormalized = new Vector2(icp.Width * pixelToNormalized, icp.Height * pixelToNormalized);
        var normalizedPrincipalPoint = icp.PrincipalPoint * pixelToNormalized;
        var normalizedToViewport = new Vector2(1 / viewportToNormalized.x, 1 / viewportToNormalized.y);

        Vector2 d = Vector2.Scale(distortedViewportPoint, viewportToNormalized);
        Vector2 o = d - normalizedPrincipalPoint;

        float K1 = (float)icp.Distortion[0];
        float K2 = (float)icp.Distortion[1];
        float P1 = (float)icp.Distortion[2];
        float P2 = (float)icp.Distortion[3];
        float K3 = (float)icp.Distortion[4];

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
        var width = icp.Width;
        var height = icp.Height;
        var principalPoint = icp.PrincipalPoint;
        var focalLength = icp.FocalLength;

        Vector2 pixelPoint = new Vector2(viewportPoint.x * width, viewportPoint.y * height);
        Vector2 offsetPoint = new Vector2(pixelPoint.x - principalPoint.x, pixelPoint.y - (height - principalPoint.y));
        Vector2 unitFocalLength = new Vector2(offsetPoint.x / focalLength.x, offsetPoint.y / focalLength.y);

        Vector3 rayDirection = cameraRotation * new Vector3(unitFocalLength.x, unitFocalLength.y, 1).normalized;

        return new Ray(cameraPos, rayDirection);
    }
}