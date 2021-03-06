﻿using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using KSP.IO;

[KSPAddon(KSPAddon.Startup.Flight, false)]
public class NavWaypoint : MonoBehaviour
{
	private NavBall navBallBehaviour;
	private GameObject indicator;

    private Color color;
    private string texture;
    private CelestialBody targetBody;
    public double latitude;
    public double longitude;
    private double height;
    public double altitude;
    private bool waypointOn;

    private static Texture2D mTexDefault;
    private static Texture2D mTexPlane;
    private static Texture2D mTexRover;

    public void SetupNavWaypoint(CelestialBody targetBody, double latitude, double longitude, double altitude, string texture, Color color)
    {
        this.targetBody = targetBody;
        this.latitude = latitude;
        this.longitude = longitude;
        this.altitude = altitude;

        switch ( texture )
        {
            case "plane":
                indicator.renderer.material.mainTexture = mTexPlane;
                break;
            case "rover":
                indicator.renderer.material.mainTexture = mTexRover;
                break;
            default:
                indicator.renderer.material.mainTexture = mTexDefault;
                break;
        }

        indicator.renderer.material.SetColor("_Color", color);

        if (targetBody.pqsController != null)
        {
            Vector3d pqsRadialVector = QuaternionD.AngleAxis(longitude, Vector3d.down) * QuaternionD.AngleAxis(latitude, Vector3d.forward) * Vector3d.right;
            height = targetBody.pqsController.GetSurfaceHeight(pqsRadialVector) - targetBody.pqsController.radius;

            if (height < 0)
                height = 0;
        }
    }

    public void Activate()
    {
        waypointOn = true;
    }

    public void Deactivate()
    {
        waypointOn = false;
    }

	public void Start()
	{
        mTexDefault = GameDatabase.Instance.GetTexture("FinePrint/Textures/default", false);
        mTexPlane = GameDatabase.Instance.GetTexture("FinePrint/Textures/plane", false);
        mTexRover = GameDatabase.Instance.GetTexture("FinePrint/Textures/rover", false);

        if (mTexDefault == null)
        {
            mTexDefault = new Texture2D(16, 16);
            mTexDefault.SetPixels32(Enumerable.Repeat((Color32)Color.magenta, 16 * 16).ToArray());
            mTexDefault.Apply();
        }

        if (mTexPlane == null)
        {
            mTexPlane = new Texture2D(16, 16);
            mTexPlane.SetPixels32(Enumerable.Repeat((Color32)Color.magenta, 16 * 16).ToArray());
            mTexPlane.Apply();
        }

        if (mTexRover == null)
        {
            mTexRover = new Texture2D(16, 16);
            mTexRover.SetPixels32(Enumerable.Repeat((Color32)Color.magenta, 16 * 16).ToArray());
            mTexRover.Apply();
        }

        this.targetBody = Planetarium.fetch.Home;
        this.latitude = 0.0;
        this.longitude = 0.0;
        this.altitude = 0.0;
        waypointOn = false;

		//Get the Navball.
		GameObject navBall = GameObject.Find("NavBall");
		Transform navBallVectorsPivotTransform = navBall.transform.FindChild("vectorsPivot");
		navBallBehaviour = navBall.GetComponent<NavBall>();

		//create alignment indicator game object
        indicator = new GameObject(name);
        Mesh m = new Mesh();
        MeshFilter meshFilter = indicator.AddComponent<MeshFilter>();
        indicator.AddComponent<MeshRenderer>();

        const float uvize = 1f;

        Vector3 p0 = new Vector3(-0.0125f, 0, 0.0125f);
        Vector3 p1 = new Vector3(0.0125f, 0, 0.0125f);
        Vector3 p2 = new Vector3(-0.0125f, 0, -0.0125f);
        Vector3 p3 = new Vector3(0.0125f, 0, -0.0125f);

        m.vertices = new[]
		{
			p0, p1, p2,
			p1, p3, p2
		};

        m.triangles = new[]
		{
			0, 1, 2,
			3, 4, 5
		};

        Vector2 uv1 = new Vector2(0, 0);
        Vector2 uv2 = new Vector2(uvize, uvize);
        Vector2 uv3 = new Vector2(0, uvize);
        Vector2 uv4 = new Vector2(uvize, 0);

        m.uv = new[]{
			uv1, uv4, uv3,
			uv4, uv2, uv3
		};

        m.RecalculateNormals();
        m.RecalculateBounds();
        m.Optimize();

        meshFilter.mesh = m;

        indicator.layer = 12;
        indicator.transform.parent = navBallVectorsPivotTransform;
        indicator.transform.localPosition = Vector3.zero;
        indicator.transform.localRotation = Quaternion.Euler(90f, 180f, 0);

        indicator.renderer.material = new Material(Shader.Find("Sprites/Default"));
        indicator.renderer.material.mainTexture = mTexDefault;
        indicator.renderer.material.SetColor("_Color", new Color(1.0f, 1.0f, 1.0f));

        indicator.SetActive(false);

        SetupNavWaypoint(Planetarium.fetch.Home, 0.0, -74.5, 1.0, "plane", new Color(0.0f, 1.0f, 0.0f));
        Deactivate();
	}

    public void LateUpdate()
    {
        if (HighLogic.LoadedSceneIsFlight)
        {
            if (FlightGlobals.fetch != null)
            {
                Vector3 worldPosition = new Vector3();
                Vector3 vesselPos;

                Vector3d surfacePos = targetBody.GetWorldSurfacePosition(latitude, longitude, height + altitude);
                worldPosition = new Vector3((float)surfacePos.x, (float)surfacePos.y, (float)surfacePos.z);

                //indicator position
                vesselPos = FlightGlobals.fetch.activeVessel.GetWorldPos3D();

                Vector3 directionToWaypoint = (worldPosition - vesselPos).normalized;
                Vector3 rotatedDirection = navBallBehaviour.attitudeGymbal * directionToWaypoint;
                indicator.transform.localPosition = rotatedDirection * navBallBehaviour.progradeVector.localPosition.magnitude;

                indicator.transform.rotation = Quaternion.Euler(90f, 180f, 180f);

                //indicator visibility (invisible if on back half sphere)
                if (waypointOn && FlightGlobals.ActiveVessel.mainBody == targetBody)
                    indicator.SetActive(indicator.transform.localPosition.z > 0.0d);
                else
                    indicator.SetActive(false);

                return;
            }
        }

        indicator.SetActive(false);
        return;
    }

    public bool isActive()
    {
        return waypointOn;
    }
}