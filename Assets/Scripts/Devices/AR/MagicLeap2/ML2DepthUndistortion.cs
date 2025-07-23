using System;
using UnityEngine;
using Unity.Collections;
using System.Runtime.InteropServices;
using MagicLeap.OpenXR.Features.PixelSensors;

public static class DepthUndistortion
{
    /// <summary>
    /// Undistorts a depth image using pinhole camera intrinsics
    /// </summary>
    /// <param name="distortedDepth">Input distorted depth data</param>
    /// <param name="width">Image width</param>
    /// <param name="height">Image height</param>
    /// <param name="intrinsics">Camera intrinsics with distortion coefficients</param>
    /// <returns>Undistorted depth data</returns>
    public static byte[] UndistortDepthImage(byte[] distortedDepth, int width, int height, PixelSensorPinholeIntrinsics intrinsics)
    {
        // Create output array
        byte[] undistortedDepth = new byte[distortedDepth.Length];
        
        // Convert to float spans for processing
        ReadOnlySpan<byte> inputByteSpan = distortedDepth.AsSpan();
        ReadOnlySpan<float> inputFloatSpan = MemoryMarshal.Cast<byte, float>(inputByteSpan);
        
        Span<byte> outputByteSpan = undistortedDepth.AsSpan();
        Span<float> outputFloatSpan = MemoryMarshal.Cast<byte, float>(outputByteSpan);
        
        // Extract intrinsic parameters
        float fx = intrinsics.FocalLength.x;
        float fy = intrinsics.FocalLength.y;
        float cx = intrinsics.PrincipalPoint.x;
        float cy = intrinsics.PrincipalPoint.y;
        
        double k1 = intrinsics.Distortion[0];
        double k2 = intrinsics.Distortion[1];
        double p1 = intrinsics.Distortion[2];
        double p2 = intrinsics.Distortion[3];
        double k3 = intrinsics.Distortion[4];
        
        // Process each pixel in the undistorted image
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Convert pixel coordinates to normalized coordinates
                double xn = (x - cx) / fx;
                double yn = (y - cy) / fy;
                
                // Apply distortion model to find corresponding distorted coordinates
                Vector2 distortedCoords = ApplyDistortion(xn, yn, k1, k2, p1, p2, k3);
                
                // Convert back to pixel coordinates
                float distortedX = (float)(distortedCoords.x * fx + cx);
                float distortedY = (float)(distortedCoords.y * fy + cy);
                
                // Bilinear interpolation to get depth value
                float depth = BilinearInterpolate(inputFloatSpan, width, height, distortedX, distortedY);
                
                // Store in output
                int outputIndex = y * width + x;
                outputFloatSpan[outputIndex] = depth;
            }
        }
        
        return undistortedDepth;
    }
    
    /// <summary>
    /// Applies the distortion model to normalized coordinates
    /// </summary>
    private static Vector2 ApplyDistortion(double xn, double yn, double k1, double k2, double p1, double p2, double k3)
    {
        double r2 = xn * xn + yn * yn;
        double r4 = r2 * r2;
        double r6 = r4 * r2;
        
        // Radial distortion
        double radialDistortion = 1 + k1 * r2 + k2 * r4 + k3 * r6;
        
        // Tangential distortion
        double tangentialX = 2 * p1 * xn * yn + p2 * (r2 + 2 * xn * xn);
        double tangentialY = p1 * (r2 + 2 * yn * yn) + 2 * p2 * xn * yn;
        
        // Apply distortion
        double xd = xn * radialDistortion + tangentialX;
        double yd = yn * radialDistortion + tangentialY;
        
        return new Vector2((float)xd, (float)yd);
    }
    
    /// <summary>
    /// Performs bilinear interpolation on the depth data
    /// </summary>
    private static float BilinearInterpolate(ReadOnlySpan<float> data, int width, int height, float x, float y)
    {
        // Handle boundary cases
        if (x < 0 || x >= width - 1 || y < 0 || y >= height - 1)
        {
            return 0f; // or some default depth value
        }
        
        int x1 = (int)Math.Floor(x);
        int y1 = (int)Math.Floor(y);
        int x2 = x1 + 1;
        int y2 = y1 + 1;
        
        // Ensure we don't go out of bounds
        x2 = Math.Min(x2, width - 1);
        y2 = Math.Min(y2, height - 1);
        
        float dx = x - x1;
        float dy = y - y1;
        
        // Get the four surrounding pixels
        float q11 = data[y1 * width + x1];
        float q12 = data[y2 * width + x1];
        float q21 = data[y1 * width + x2];
        float q22 = data[y2 * width + x2];
        
        // Bilinear interpolation
        float top = q11 * (1 - dx) + q21 * dx;
        float bottom = q12 * (1 - dx) + q22 * dx;
        
        return top * (1 - dy) + bottom * dy;
    }
    
    /// <summary>
    /// Creates a lookup table for faster undistortion (recommended for real-time processing)
    /// </summary>
    public static void CreateUndistortionLookupTable(int width, int height, PixelSensorPinholeIntrinsics intrinsics, 
        out Vector2[] lookupTable)
    {
        lookupTable = new Vector2[width * height];
        
        float fx = intrinsics.FocalLength.x;
        float fy = intrinsics.FocalLength.y;
        float cx = intrinsics.PrincipalPoint.x;
        float cy = intrinsics.PrincipalPoint.y;
        
        double k1 = intrinsics.Distortion[0];
        double k2 = intrinsics.Distortion[1];
        double p1 = intrinsics.Distortion[2];
        double p2 = intrinsics.Distortion[3];
        double k3 = intrinsics.Distortion[4];
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double xn = (x - cx) / fx;
                double yn = (y - cy) / fy;
                
                Vector2 distortedCoords = ApplyDistortion(xn, yn, k1, k2, p1, p2, k3);
                
                float distortedX = (float)(distortedCoords.x * fx + cx);
                float distortedY = (float)(distortedCoords.y * fy + cy);
                
                lookupTable[y * width + x] = new Vector2(distortedX, distortedY);
            }
        }
    }
    
    /// <summary>
    /// Fast undistortion using pre-computed lookup table
    /// </summary>
    public static byte[] UndistortDepthImageFast(byte[] distortedDepth, int width, int height, Vector2[] lookupTable)
    {
        byte[] undistortedDepth = new byte[distortedDepth.Length];
        
        ReadOnlySpan<byte> inputByteSpan   = distortedDepth.AsSpan();
        ReadOnlySpan<float> inputFloatSpan = MemoryMarshal.Cast<byte, float>(inputByteSpan);
        
        Span<byte> outputByteSpan = undistortedDepth.AsSpan();
        Span<float> outputFloatSpan = MemoryMarshal.Cast<byte, float>(outputByteSpan);
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 distortedCoords = lookupTable[y * width + x];
                float depth = BilinearInterpolate(inputFloatSpan, width, height, distortedCoords.x, distortedCoords.y);
                outputFloatSpan[y * width + x] = depth;
            }
        }
        
        return undistortedDepth;
    }
}