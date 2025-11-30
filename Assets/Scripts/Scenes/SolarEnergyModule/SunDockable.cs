using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Unity.Netcode;
using Unity.Mathematics;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(LineRenderer))]
public class SunDockable : Dockable
{
    [Header("Solar Panel Default Configuration")]
    [SerializeField] private float panelEfficiency = 0.18f;
    [SerializeField] private float panelArea = 2f;

    [Header("Beam Visual Settings")]
    [SerializeField] private float beamStartWidth = 0.07f;
    [SerializeField] private float beamEndWidth = 0.1f;

    [Header("UI References")]
    [SerializeField] private Text angleText;
    [SerializeField] private Text powerText;
    [SerializeField] private Transform powerPointer;
    [SerializeField] private Text lightText;
    [SerializeField] private Slider lightSlider;
    [SerializeField] private Transform cloudParent;

    [Header("Cloud Animation Settings")]
    [SerializeField] private float cloudRiseDistance = 1.5f;
    [SerializeField] private float cloudAppearDuration = 0.5f;
    [SerializeField] private float cloudDisappearDuration = 0.2f;

    private LineRenderer _beam;
    private float _minPower;
    private float _maxPower;

    private NetworkVariable<float> _angle = new NetworkVariable<float>(
        90f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    private NetworkVariable<float> _light = new NetworkVariable<float>(
        1.0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    private NetworkVariable<float> _power = new NetworkVariable<float>(
        1.0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    public override void PrepareComponents()
    {
        // Calculate power bounds
        _minPower = 0f;
        _maxPower = 800f * panelArea * panelEfficiency;

        // Setup beam
        _beam = GetComponent<LineRenderer>();
        _beam.startWidth = beamStartWidth;
        _beam.endWidth = beamEndWidth;

        // Setup network variable callbacks
        _light.OnValueChanged += OnLightChanged;
        _angle.OnValueChanged += OnAngleChanged;
        _power.OnValueChanged += OnPowerChanged;
    }

    private void OnLightChanged(float oldValue, float newValue)
    {
        if (lightSlider != null)
            lightSlider.SetValueWithoutNotify(newValue);

        if (lightText != null)
            lightText.text = $"{Mathf.Round(newValue * 100)}%";

        UpdateCloudVisibility();
    }

    private void OnAngleChanged(float oldValue, float newValue)
    {
        if (angleText != null)
            angleText.text = (newValue <= 90) ? $"{newValue}°" : "-";
    }

    private void OnPowerChanged(float oldValue, float newValue)
    {
        float power = Mathf.Max(_minPower, newValue);

        if (powerText != null)
            powerText.text = $"{Mathf.Round(power * 10f) / 10f}W";

        if (powerPointer != null) {
            float pointerRotation = math.remap(_minPower, _maxPower, 120, -120, power);
            powerPointer.localRotation = Quaternion.Euler(0, 0, pointerRotation);
        }
    }

    public override void UpdateComponents()
    {
        if (IsServer) {
            CalculateIncidenceAngle();
            CalculateSolarPowerOutput();
        }

        UpdateBeamVisual();
    }

    private void UpdateBeamVisual()
    {
        if (_beam == null || spawnable == null) return;


        Debug.Log("Hi" + _light.Value);
        Transform panel = spawnable.transform;
        bool isAngleValid = _angle.Value <= 90;

        _beam.enabled = isAngleValid;

        if (isAngleValid) {
            _beam.SetPosition(0, transform.position);
            _beam.SetPosition(1, panel.position);

            // Adjust beam width and transparency based on light intensity
            _beam.endWidth     = Mathf.Max(beamEndWidth * (1f - _light.Value), beamStartWidth);
            Color currentColor = _beam.startColor;
            currentColor.a     = 1f * (1f - _light.Value);
            _beam.startColor   = currentColor;
        }
    }

    private void CalculateSolarPowerOutput()
    {
        float directNormalIrradiance = 800f * Mathf.Exp(-3f * _light.Value);
        float incidenceFactor = Mathf.Cos(_angle.Value * Mathf.Deg2Rad);

        _power.Value = directNormalIrradiance * incidenceFactor * panelArea * panelEfficiency;
    }

    private void CalculateIncidenceAngle()
    {
        if (spawnable == null) return;

        /* The goal is to achieve:
        *   Sun directly above solar panel                → 0°
        *   Sun leveled with the panel ("on the horizon") → 90°
        *   Sun anywhere "above horizon"                  → ]0°,  90°[
        *   Sun anywhere "under horizon"                  → ]0°, -90°[  */
        Transform panel = spawnable.transform;
        Vector3 sunDirection = panel.InverseTransformDirection(
            (transform.position - panel.position).normalized
        );

        //                                       v Calculates elevation angle from horizontal
        _angle.Value = Mathf.Round(90f - Mathf.Asin(sunDirection.y) * Mathf.Rad2Deg);
        //                                                                  ^ converts from radians to degrees
    }

    public void SetLightPercentage(float value)
    {
        if (_light.Value == value) return;
        UpdateLightServerRpc(value);
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdateLightServerRpc(float value)
    {
        _light.Value = value;
    }

    private void UpdateCloudVisibility()
    {
        if (cloudParent == null) return;

        int totalClouds = cloudParent.childCount;
        int cloudsToEnable = Mathf.RoundToInt(totalClouds * _light.Value);

        for (int i = 0; i < totalClouds; i++) {
            GameObject cloud = cloudParent.GetChild(i).gameObject;

            if (i < cloudsToEnable) ShowCloud(cloud);
            else HideCloud(cloud);
        }
    }

    private void ShowCloud(GameObject cloud)
    {
        if (!cloud.activeSelf)
        {
            Vector3 topPosition = cloud.transform.localPosition;
            Vector3 bottomPosition = topPosition + new Vector3(0, -cloudRiseDistance, 0);

            cloud.transform.localPosition = bottomPosition;
            cloud.SetActive(true);
            cloud.transform.DOLocalMove(topPosition, cloudAppearDuration);
        }
    }

    private void HideCloud(GameObject cloud)
    {
        if (!DOTween.IsTweening(cloud.transform) && cloud.activeSelf)
        {
            Vector3 topPosition = cloud.transform.localPosition;
            Vector3 bottomPosition = topPosition + new Vector3(0, -cloudRiseDistance, 0);

            cloud.transform.DOLocalMove(bottomPosition, cloudDisappearDuration)
                .OnComplete(() =>
                {
                    cloud.SetActive(false);
                    cloud.transform.localPosition = topPosition;
                });
        }
    }
}