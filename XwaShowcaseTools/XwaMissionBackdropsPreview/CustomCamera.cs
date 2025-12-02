using JeremyAnsel.DirectX.DXMath;
using JeremyAnsel.DirectX.SdkCamera;
using JeremyAnsel.DirectX.Window;
using System;

namespace XwaMissionBackdropsPreview;

internal sealed class CustomCamera
{
    private readonly SdkArcBall m_ArcBall = new();

    private float m_fFOV;
    private float m_fAspect;
    private float m_fNearPlane;
    private float m_fFarPlane;

    private float m_zoom;

    public CustomCamera()
    {
        Reset();
    }

    public void HandleMessages(IntPtr hWnd, WindowMessageType msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WindowMessageType.MouseWheel)
        {
            int wheelDelta = (short)((ulong)wParam >> 16);
            float delta = wheelDelta * 0.03f / 120.0f;
            SetZoom(GetZoom() + delta);
            return;
        }

        m_ArcBall.HandleMessages(hWnd, msg, wParam, lParam);
    }

    public void Reset()
    {
        m_ArcBall.Reset();
        m_zoom = 1.0f;
    }

    public void SetViewParams(XMVector vEyePt, XMVector vLookatPt)
    {
        XMMatrix mRotation = XMMatrix.LookAtLH(vEyePt, vLookatPt, XMVector.FromFloat(0.0f, 1.0f, 0.0f, 0.0f));
        XMVector quat = XMQuaternion.RotationMatrix(mRotation);
        m_ArcBall.SetQuatNow(quat);
    }

    public void SetProjParams(float fFOV, float fAspect, float fNearPlane, float fFarPlane)
    {
        m_fFOV = Math.Clamp(fFOV, 0.1f, XMMath.PIDivTwo);
        m_fAspect = fAspect;
        m_fNearPlane = fNearPlane;
        m_fFarPlane = fFarPlane;
    }

    public void SetWindow(int nWidth, int nHeight, float fArcballRadius)
    {
        m_ArcBall.SetWindow(nWidth, nHeight, fArcballRadius);
    }

    public void SetQuat(XMVector q)
    {
        m_ArcBall.SetQuatNow(q);
    }

    public float GetZoom()
    {
        return m_zoom;
    }

    public void SetZoom(float zoom)
    {
        if (m_fFOV <= 0.0f)
        {
            m_zoom = 0.1f;
            return;
        }

        m_zoom = Math.Clamp(zoom, 0.1f * m_fFOV, XMMath.PI * m_fFOV);
    }

    public XMMatrix GetTransformMatrix()
    {
        return m_ArcBall.GetRotationMatrix();
    }

    public XMMatrix GetProjMatrix()
    {
        float fFOV = Math.Clamp(m_fFOV * m_zoom, 0.1f, XMMath.PI);
        XMMatrix proj = XMMatrix.PerspectiveFovLH(fFOV, m_fAspect, m_fNearPlane, m_fFarPlane);
        return proj;
    }
}
