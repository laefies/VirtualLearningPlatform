using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(MeshRenderer))]
public class SunInteractable : Interactable
{
    // TODO » Melhorar raio visualmente;
    private LineRenderer _beam;
    private float _angle = 90;
    private float _light = 0.8f;

    public Text _angleText;
    public Text _lightText;

    public override void PrepareComponents()
    {
        _beam = GetComponent<LineRenderer>();
        _beam.startWidth = 0.01f;
        _beam.endWidth   = 0.03f;
        // _beam.startColor = Color.red;
        // _beam.endColor   = Color.red;
    }

    public override void UpdateComponents()
    {
        MarkerInfo panel = spawnable.GetMarkerInfo();

        _beam.enabled  = true;
        _beam.SetPosition(0, transform.position);
        _beam.SetPosition(1, panel.Pose.position);

        GetIncidenceAngle(panel);
    }

    public void GetIncidenceAngle(MarkerInfo panel)
    {
        Vector3 panelNormal = panel.Pose.rotation * Vector3.up;
        Vector3 sunToPanel  = (panel.Pose.position - transform.position).normalized;

        float angleRad = Mathf.Acos(Vector3.Dot(panelNormal, sunToPanel));

        _angle = 180-Mathf.Round(angleRad * Mathf.Rad2Deg);
        _angleText.text = $"{_angle}°";
    }

    public void SetLightPercentage(float value) {
        _light = value;
        _lightText.text = $"{Mathf.Round(_light * 100)}%";
    }
}