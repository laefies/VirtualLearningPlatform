using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(MeshRenderer))]
public class SunInteractable : Interactable
{
    // TODO » Melhorar raio visualmente;
    private LineRenderer _beam;
    private float _angle = 90;
    private float _light = 0.8f;

    [SerializeField] private Text _angleText;
    [SerializeField] private Text _lightText;
    [SerializeField] private Transform cloudParent;

    public override void PrepareComponents()
    {
        _beam = GetComponent<LineRenderer>();
        _beam.startWidth = 0.03f;
        _beam.endWidth = 0.05f;
    }

    public override void UpdateComponents()
    {
        MarkerInfo panel = spawnable.GetMarkerInfo();

        _beam.enabled = true;
        _beam.SetPosition(0, transform.position);
        _beam.SetPosition(1, panel.Pose.position);

        _beam.endWidth = Mathf.Max(0.05f * (1f - _light), 0.03f);

        Color currentColor = _beam.startColor;
        currentColor.a = 1f * (1f - _light);
        _beam.startColor = currentColor;

        GetIncidenceAngle(panel);
    }

    public void GetIncidenceAngle(MarkerInfo panel)
    {
        Vector3 panelNormal = panel.Pose.rotation * Vector3.up;
        Vector3 sunToPanel = (panel.Pose.position - transform.position).normalized;

        float angleRad = Mathf.Acos(Vector3.Dot(panelNormal, sunToPanel));

        _angle = 180 - Mathf.Round(angleRad * Mathf.Rad2Deg);
        _angleText.text = $"{_angle}°";
    }

    public void UpdateCloudVisibility(float percentage)
    {
        if (cloudParent == null) return;

        int totalClouds = cloudParent.childCount;
        int cloudsToEnable = Mathf.RoundToInt(totalClouds * percentage);

        for (int i = 0; i < totalClouds; i++)
        {
            GameObject cloud = cloudParent.GetChild(i).gameObject;
            if (i < cloudsToEnable)
            {
                if (!cloud.activeSelf)
                {
                    Vector3 topPosition    = cloud.transform.localPosition;
                    Vector3 bottomPosition = topPosition + new Vector3(0, 0, 1.5f); 
                    cloud.transform.localPosition = bottomPosition;
                    cloud.SetActive(true);
                    cloud.transform.DOLocalMove(topPosition, 0.5f);
                }
            }
            else
            {
                if (!DOTween.IsTweening(cloud.transform) && cloud.activeSelf) {
                    Vector3 topPosition    = cloud.transform.localPosition;
                    Vector3 bottomPosition = topPosition + new Vector3(0, 0, 1.5f); 

                    cloud.transform.DOLocalMove(bottomPosition, 0.2f).OnComplete(() =>
                    {
                        cloud.SetActive(false);
                        cloud.transform.localPosition = topPosition;
                    });

                }
            }
        }
    }

    public void SetLightPercentage(float value)
    {
        _light = value;
        _lightText.text = $"{Mathf.Round(_light * 100)}%";
        UpdateCloudVisibility(_light);
    }

}