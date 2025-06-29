using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Reflection.Metadata;
using Ab4d.SharpEngine;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Materials;
using Ab4d.SharpEngine.SceneNodes;
using Ab4d.SharpEngine.Transformations;
using Ab4d.SharpEngine.Utilities;
using Ab4d.SpaceSimulator.Physics;

namespace Ab4d.SpaceSimulator.Visualization;

[DebuggerDisplay("CelestialBodyView({Name})")]
public class CelestialBodyView
{
    private readonly VisualizationEngine _visualizationEngine;
    public readonly CelestialBody CelestialBody;

    public string Name => CelestialBody.Name;
    public CelestialBodyType Type => CelestialBody.Type;

    // Parent / child hierarchy
    public CelestialBodyView? Parent = null;
    public readonly List<CelestialBodyView> Children = [];

    // Celestial body sphere
    public readonly SphereModelNode SphereNode;

    // Fixed trajectory / orbit
    public readonly EllipseLineNode? OrbitNode;
    private Vector3d _centerOffset;

    // Dynamic trajectory / trail
    public readonly MultiLineNode? TrajectoryTrailNode;

    // Ring(s)
    public readonly CircleModelNode[]? RingNodes = null;

    // Axes
    public readonly AxisLineNode? OrbitalAxesNode;
    public readonly AxisLineNode? RotationalAxesNode;

    // Pre-computed DCM matrix that corresponds to orbital parameters. Used to transform the ellipse's direction vectors,
    // as well as to compute transform for sphere node, ring(s) and orbital/rotational axes.
    private readonly Matrix4x4 _orbitDcm;

    public bool ShowName = true;
    public SceneNode? NameSceneNode { get; set; }

    public float DistanceToCamera { get; private set; }


    private Color3 _orbitColor = new Color3(0.25f, 0.25f, 0.25f);

    public Color3 OrbitColor
    {
        get => _orbitColor;
        set
        {
            _orbitColor = value;

            UpdateOrbitColor();

            if (TrajectoryTrailNode != null)
                UpdateTrajectoryTrailColor(true); // Force the update due to color change
        }
    }

    public CelestialBodyView(VisualizationEngine engine, CelestialBody physicsObject, Material material, PlanetTextureLoader? textureLoader = null)
    {
        _visualizationEngine = engine;

        // Store reference to object from physics engine
        CelestialBody = physicsObject;

        // Pre-compute the DCM (directional cosine matrix) for the orbit.
        // Orientation of the ellipse is determined from Kepler elements - longitude of ascending node (Omega),
        // inclination (i), and argument of periapsis (omega), which can be interpreted as 3-1-3 Euler angles,
        // and used to rotate the base axis vectors.
        if (CelestialBody.HasOrbit)
        {
            _orbitDcm =
                Matrix4x4.CreateFromAxisAngle(Vector3.UnitZ,float.DegreesToRadians((float)CelestialBody.ArgumentOfPeriapsis)) *
                Matrix4x4.CreateFromAxisAngle(Vector3.UnitX, float.DegreesToRadians((float)CelestialBody.OrbitalInclination)) *
                Matrix4x4.CreateFromAxisAngle(Vector3.UnitZ, float.DegreesToRadians((float)CelestialBody.LongitudeOfAscendingNode));
        }
        else
        {
            _orbitDcm = Matrix4x4.Identity;
        }

        // Create sphere node
        SphereNode = new SphereModelNode(name: $"{this.Name}-Sphere")
        {
            Material = material,
        };

        if (physicsObject.HasOrbit)
        {
            OrbitalAxesNode = new AxisLineNode(length: 1, name: $"{this.Name}-OrbitalAxes");
            RotationalAxesNode = new AxisLineNode(length: 1, name: $"{this.Name}-RotationalAxes");
        }

        // Orbit ellipse
        if (CelestialBody.HasOrbit && CelestialBody.Parent != null)
        {
            var orbitColor = new Color3(0.2f, 0.2f, 0.2f);  // This is the default color that can be changed by setting OrbitColor

            // The given orbital radius is interpreted as the major semi-axis; from it and eccentricity, we can compute the
            // minor semi-axis.
            var majorSemiAxis = (float)ScaleDistance(CelestialBody.OrbitRadius);
            var minorSemiAxis = majorSemiAxis * MathF.Sqrt(1 - (float)(CelestialBody.OrbitalEccentricity * CelestialBody.OrbitalEccentricity)); // b = a * sqrt(1 - e^2)

            var majorSemiAxisDir = Vector3.Transform(Vector3.UnitX, _orbitDcm);
            var minorSemiAxisDir = Vector3.Transform(Vector3.UnitY, _orbitDcm);

            // For eccentric orbits, we need to shift the center so that the parent is placed into one of its foci.
            // The shift needs to be done along the transformed major semi-axis.
            //
            // NOTE: the offset is stored, so that it can be re-applied in UpdateVisualization().
            var c = CelestialBody.OrbitalEccentricity * CelestialBody.OrbitRadius; // c = a * e
            _centerOffset = -c * new Vector3d(majorSemiAxisDir.X, majorSemiAxisDir.Y, majorSemiAxisDir.Z);

            // We have a coordinate system mismatch between the ecliptic coordinate system (used by the computational
            // part of our codebase) and display coordinate system. See TransformPosition() for details.
            // Therefore, the semi-axis direction vectors need to be transformed/corrected before being passed to
            // the display node.
            majorSemiAxisDir = new Vector3(majorSemiAxisDir.X, majorSemiAxisDir.Z, -majorSemiAxisDir.Y);
            minorSemiAxisDir = new Vector3(minorSemiAxisDir.X, minorSemiAxisDir.Z, -minorSemiAxisDir.Y);

            OrbitNode = new EllipseLineNode(
                orbitColor,
                1,
                name: $"{this.Name}-OrbitEllipse")
            {
                CenterPosition = TransformPosition(CelestialBody.Parent.Position + _centerOffset),
                WidthDirection = majorSemiAxisDir,
                Width = majorSemiAxis * 2,
                HeightDirection = minorSemiAxisDir,
                Height = minorSemiAxis * 2,
                Segments = 359, // 1-degree resolution
            };
        }

        // Trail / trajectory for objects with parent (planets and moons)
        if (CelestialBody.TrajectoryTracker != null)
        {
            // Create trajectory multi-line node
            var initialTrajectory = GetTrajectoryTrail();
            TrajectoryTrailNode = new MultiLineNode(
                initialTrajectory,
                true,
                new PositionColoredLineMaterial($"{this.Name}-PositionColoredLineMaterial")
                {
                    LineThickness = 2,
                },
                name: $"{this.Name}-Trajectory");

            UpdateTrajectoryTrailColor(); // Initial color update
        }

        // Ring(s)
        if (CelestialBody.Rings != null)
        {
            RingNodes = new CircleModelNode[CelestialBody.Rings.Count];
            for (var i = 0; i < CelestialBody.Rings.Count; i++)
            {
                var ringInfo = CelestialBody.Rings[i];

                // Create material
                var ringMaterial = new SolidColorMaterial(ringInfo.BaseColor, name: $"{this.Name}-{ringInfo.Name}-Material");
                if (ringInfo.TextureName != null && textureLoader != null)
                    textureLoader.LoadPlanetTextureAsync(ringInfo.TextureName, ringMaterial);

                // Create circle model node
                RingNodes[i] = new CircleModelNode(name: $"{this.Name}-{ringInfo.Name}")
                {
                    CenterPosition = SphereNode.CenterPosition,
                    InnerRadius = (float)ScaleDistance(ringInfo.InnerRadius),
                    Radius = (float)ScaleDistance(ringInfo.OuterRadius),
                    // By default, the ring circle lies in the X-Y visualization plane. UpdateVisualization() will
                    // apply tilt transform, so that the ring's tilt will match that of the celestial body's sphere node.
                    Normal = Vector3.UnitY,
                    UpDirection = Vector3.UnitX,
                    // Ensure ring is visible from both sides.
                    Material = ringMaterial,
                    BackMaterial = ringMaterial,
                    TextureMappingType = CircleTextureMappingTypes.RadialFromInnerRadius,
                    Segments = 359,
                };
            }
        }

        // Perform initial update
        UpdatePhysicalProperties();
        UpdateVisualization();
    }

    public void RegisterNodes(GroupNode rootNode)
    {
        rootNode.Add(SphereNode);
        if (OrbitNode != null)
        {
            rootNode.Add(OrbitNode);
        }
        if (TrajectoryTrailNode != null)
        {
            rootNode.Add(TrajectoryTrailNode);
        }
        if (RingNodes != null)
        {
            foreach (var ringNode in RingNodes)
            {
                rootNode.Add(ringNode);
            }
        }

        if (OrbitalAxesNode != null)
            rootNode.Add(OrbitalAxesNode);
        if (RotationalAxesNode != null)
            rootNode.Add(RotationalAxesNode);

    }

    // Update properties to reflect the change in underlying physical object properties (e.g., position).
    public void UpdatePhysicalProperties()
    {
        // Update position from the underlying physical object
        SphereNode.CenterPosition = TransformPosition(CelestialBody.Position);

        if (OrbitalAxesNode != null)
            OrbitalAxesNode.Position = SphereNode.CenterPosition;
        if (RotationalAxesNode != null)
            RotationalAxesNode.Position = SphereNode.CenterPosition;

        // Transform orbital (ecliptic) axes visualization
        if (OrbitalAxesNode != null)
        {
            var transform = new MatrixTransform(
                Matrix4x4.CreateTranslation(-SphereNode.CenterPosition) *
                Matrix4x4.CreateFromAxisAngle(Vector3.UnitX, float.DegreesToRadians(90)) *
                _orbitDcm *
                Matrix4x4.CreateFromAxisAngle(Vector3.UnitX, -float.DegreesToRadians(90)) *
                Matrix4x4.CreateTranslation(SphereNode.CenterPosition));
            OrbitalAxesNode.Transform = transform;
        }

        // Transform rotational axes visualization - and apply the same transform to the sphere node, so we can use
        // the axes for visualization/debugging of the sphere's orientation.
        var compositeMatrix = (
            Matrix4x4.CreateTranslation(-SphereNode.CenterPosition) *
            Matrix4x4.CreateFromAxisAngle(Vector3.UnitY, float.DegreesToRadians((float)CelestialBody.Rotation)) *
            Matrix4x4.CreateFromAxisAngle(Vector3.UnitZ, -float.DegreesToRadians((float)CelestialBody.AxialTilt)) *
            Matrix4x4.CreateFromAxisAngle(Vector3.UnitX, float.DegreesToRadians(90)) *
            _orbitDcm *
            Matrix4x4.CreateFromAxisAngle(Vector3.UnitX, -float.DegreesToRadians(90)) *
            Matrix4x4.CreateTranslation(SphereNode.CenterPosition)
        );
        SphereNode.Transform = new MatrixTransform(compositeMatrix);

        if (RotationalAxesNode != null)
            RotationalAxesNode.Transform = SphereNode.Transform;

        // Update orbit ellipse - its position
        if (OrbitNode != null && CelestialBody.Parent != null)
        {
            // NOTE: strictly speaking, we should scale using parent's ScalePosition(), in case it uses different
            // parameters...
            // NOTE2: we apply pre-computed center offset for the eccentric orbit; this implicitly assumes that the
            // orbital parameters (from which the offset was computed) do not change with time. If we wanted to implement
            // time-based change (e.g., using almanac to periodically update orbital parameters), we would need to
            // perform all computations here (or on the orbital parameter update).
            OrbitNode.CenterPosition = TransformPosition(CelestialBody.Parent.Position + _centerOffset);
        }

        // Update celestial body's trail (positions), and its color data (alpha blending depends on number of positions).
        if (CelestialBody.TrajectoryTracker != null && TrajectoryTrailNode != null)
        {
            TrajectoryTrailNode.Positions = GetTrajectoryTrail();
            UpdateTrajectoryTrailColor();
        }

        // Update ring(s), if applicable - move their centers.
        if (RingNodes != null)
        {
            foreach (var ringNode in RingNodes)
            {
                ringNode.CenterPosition = SphereNode.CenterPosition;

                // Apply the same tilt/rotation transform; the ring should be rotationally invariant, so only the tilt
                // part should be applicable.
                ringNode.Transform = SphereNode.Transform;
            }
        }
    }

    // Finalize the visualization update - this updates properties that depend on both celestial body's physical properties
    // (e.g., position) and camera position/orientation. Therefore, this update must be performed *after* UpdatePhysicalProperties()
    // is called, and after any potential changes are made to camera (for example, if camera's position and orientation
    // are set by tracking of the celestial body).
    public void UpdateVisualization()
    {
        var camera = _visualizationEngine.Camera;
        var viewWidth = _visualizationEngine.SceneView.Width;

        // Adapted from CameraUtils.GetPerspectiveScreenSize()
        var distanceVector = SphereNode.CenterPosition - camera.GetCameraPosition();
        var lookDirection = Vector3.Normalize(camera.GetLookDirection());
        var distanceToCamera = Vector3.Dot(distanceVector, lookDirection);

        bool isBodyVisible;
        bool forcedScaling = false;

        if (viewWidth > 0)
        {
            // Dynamic size change - can be triggered by other changes, such as viewport
            float sphereRadius = ScaleSize(CelestialBody.Radius);

            var xScale = MathF.Tan(camera.FieldOfView * MathF.PI / 360);

            if (_visualizationEngine.IsMinSizeLimited)
            {
                var displayedSize = viewWidth * sphereRadius / (distanceToCamera * xScale); // We would also need to multiply sphereRadius * 2, and also multiply xScale * 2; but in this case we can skip that

                var minSize = _visualizationEngine.MinScreenSize;
                if (displayedSize < minSize)
                {
                    var correctedSize = distanceToCamera * xScale * minSize / viewWidth; // Inverted eq. for displayedSize

                    if (correctedSize > 0)
                    {
                        sphereRadius = correctedSize;
                        forcedScaling = true; // Force rescaling of rings to match the size limit
                    }
                }
            }

            SphereNode.Radius = sphereRadius;

            if (this.Parent != null && this.Parent.SphereNode != null)
            {
                var orbitRadius = (float)this.CelestialBody.OrbitRadius * VisualizationEngine.ViewUnitScale;
                var orbitRadiusScreenSize = (orbitRadius * viewWidth) / (distanceToCamera * xScale * 2);

                var parentRadius = this.Parent.SphereNode.Radius;

                isBodyVisible = orbitRadius > (parentRadius + sphereRadius) * 1.1 &&            // If orbit is too close to parent, then hide the planet or moon
                                orbitRadiusScreenSize > VisualizationEngine.MinOrbitScreenSize; // If moon or planet's orbit is smaller than 20 pixels, then hide the planet or moon

                bool isOrbitVisible = isBodyVisible && (orbitRadius > parentRadius * 1.1);

                SphereNode.Visibility = isBodyVisible ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden;

                if (OrbitNode != null)
                    OrbitNode.Visibility = (isOrbitVisible && _visualizationEngine.ShowOrbits) ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden;

                if (TrajectoryTrailNode != null)
                    TrajectoryTrailNode.Visibility = (isOrbitVisible && _visualizationEngine.ShowTrails) ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden;
            }
            else
            {
                isBodyVisible = true;
            }
        }
        else
        {
            isBodyVisible = false; // viewWidth == 0
        }

        if (NameSceneNode != null)
            NameSceneNode.Visibility = (isBodyVisible && _visualizationEngine.ShowNames && ShowName) ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden;

        if (RingNodes != null && CelestialBody.Rings != null)
        {
            for (var i = 0; i < RingNodes.Length; i++)
            {
                var ringNode = RingNodes[i];
                var ringInfo = CelestialBody.Rings[i];

                if (forcedScaling)
                {
                    // Forced minimal planet size; rescale the ring radii accordingly.
                    ringNode.InnerRadius = SphereNode.Radius * (float)(ringInfo.InnerRadius / CelestialBody.Radius);
                    ringNode.Radius = SphereNode.Radius * (float)(ringInfo.OuterRadius / CelestialBody.Radius);
                }
                else
                {
                    // Native scaling
                    ringNode.InnerRadius = (float)ScaleDistance(ringInfo.InnerRadius);
                    ringNode.Radius = (float)ScaleDistance(ringInfo.OuterRadius);
                }

                // Toggle visibility
                ringNode.Visibility = isBodyVisible ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden;
            }
        }

        // Scaling and visibility of orbital/rotational axes
        if (OrbitalAxesNode != null)
        {
            OrbitalAxesNode.Length = SphereNode.Radius * 2;
            OrbitalAxesNode.Visibility = (isBodyVisible && _visualizationEngine.ShowOrbitalAxes) ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden;
        }
        if (RotationalAxesNode != null) {
            RotationalAxesNode.Length = SphereNode.Radius * 2;
            RotationalAxesNode.Visibility = (isBodyVisible && _visualizationEngine.ShowRotationalAxes) ? SceneNodeVisibility.Visible : SceneNodeVisibility.Hidden;
        }

        this.DistanceToCamera = isBodyVisible ? distanceToCamera : 0;

        UpdateNameSceneNode();
    }

    public void UpdateOrbitColor()
    {
        if (OrbitNode != null)
            OrbitNode.LineColor = (this.OrbitColor * _visualizationEngine.OrbitColorMultiplier).ToColor4(); // Make orbit color darker
    }

    private void UpdateTrajectoryTrailColor(bool force = false)
    {
        if (TrajectoryTrailNode?.Material is not PositionColoredLineMaterial positionColoredLineMaterial)
            return;

        var numColors = positionColoredLineMaterial.PositionColors?.Length ?? 0;
        var numPositions = TrajectoryTrailNode.Positions?.Length ?? 0;

        // Since color alpha is computed based on number of positions (rather the distance or angle between them), we
        // can avoid an update when the number of positions remains unchanged; unless the update is forced due to
        // orbit color change.
        if (numPositions == numColors && !force)
            return;

        var positionColors = new Color4[numPositions];
        for (var i = 0; i < numPositions; i++)
        {
            positionColors[i] = new Color4(_visualizationEngine.TrailColorMultiplier * OrbitColor, (float)i / numPositions); // Base color is lighter than orbit's color; alpha is based on position.
        }

        positionColoredLineMaterial.PositionColors = positionColors;

        // Workaround for the issue with transparency in PositionColoredLineMaterial (this will be solved in v3.1)
        positionColoredLineMaterial.LineColor = new Color4(1, 1, 1, 0.99f); // Force using alpha blending by setting LineColor.Alpha to less than 1
    }

    private double ScaleDistance(double distance)
    {
        // Scale so 1 unit in 3D view space is 1 million km = 1e9 m
        return distance * VisualizationEngine.ViewUnitScale;
    }

    // Scale the position, as well as apply transformation from ecliptic coordinate system to display one.
    // The ecliptic coordinate system: X axis pointing out, Y axis pointing right, Z axis pointing up
    // Display coordinate system: X axis pointing out, Y axis pointing up, Z axis pointing left
    // Thus: Xd = Xe, Yd = Ze, Zd = -Ye
    private Vector3 TransformPosition(Vector3d realPosition)
    {
        var length = realPosition.Length();
        var scaledLength = ScaleDistance(length);

        var scaledPosition = scaledLength > 0 ? scaledLength * Vector3d.Normalize(realPosition) : Vector3d.Zero;
        return new Vector3((float)scaledPosition.X, (float)scaledPosition.Z, -(float)scaledPosition.Y);
    }

    private float ScaleSize(double realSize)
    {
        // Scale so 1 unit in 3D view space is 1 million km = 1e9 m
        var scaledSize = (float)(realSize * VisualizationEngine.ViewUnitScale);

        // Scale by global scale factor
        scaledSize *= _visualizationEngine.CelestialBodyScaleFactor;

        return scaledSize;
    }

    private Vector3[] GetTrajectoryTrail()
    {
        if (CelestialBody.TrajectoryTracker == null)
            return [];

        var data = CelestialBody.TrajectoryTracker.GetTrajectoryData();
        var trajectory = new Vector3[data.Count + 1]; // Always append current position

        var idx = 0;
        if (CelestialBody.Parent != null /* && !forceHeliocentricTrajectories */)
        {
            // Parent-centric trajectory
            var currentParentPosition = CelestialBody.Parent.Position;
            foreach (var entry in data)
            {
                var position = currentParentPosition + (entry.Position - entry.ParentPosition);
                trajectory[idx++] = TransformPosition(position);
            }
        }
        else
        {
            // Helio-centric trajectory
            foreach (var entry in data)
            {
                trajectory[idx++] = TransformPosition(entry.Position);
            }
        }

        // Add current position - this prevents "gaps" between the last tracked position and the current position
        // (current position might not be tracked due to its revolution angle being below the threshold), and makes
        // the trajectory update appear to be smooth.
        trajectory[idx++] = TransformPosition(CelestialBody.Position);

        return trajectory;
    }

    public void UpdateNameSceneNode()
    {
        if (NameSceneNode == null)
            return;

        var desiredScreenHeight = 16f;

        // If we want to specify the screen size in device independent units, then we need to scale by DPI scale.
        // If we want to set the size in pixels, the comment the following line.
        desiredScreenHeight *= _visualizationEngine.SceneView.DpiScaleX;

        var camera = _visualizationEngine.Camera;

        var viewWidth = (float)_visualizationEngine.SceneView.Width;
        var viewHeight = (float)_visualizationEngine.SceneView.Height;


        // Get lookDirectionDistance - re-use the distance from camera to celestial body.
        var lookDirectionDistance = this.DistanceToCamera;

        float aspectRatio = viewWidth / viewHeight;
        float xScale = MathF.Tan(camera.FieldOfView * MathF.PI / 360);

        float scaleFactor = (2 * lookDirectionDistance * desiredScreenHeight * xScale) / (aspectRatio * viewHeight);


        // To align the text with camera, we first need to generate the text
        // so that its textDirection is set to (1, 0, 0) and upDirection is set to (0, 1, 0).
        // This will orient the text with the camera when Heading is 0 and Attitude is 0.
        // After that, we can align the text with the camera by simply negating the camera's
        // rotation that is defined by view matrix.

        var invertedView = camera.GetInvertedViewMatrix();

        // Get the right direction vector from the inverted view matrix
        var rightDirectionVector = new Vector3(invertedView.M11, invertedView.M12, invertedView.M13);

        // Move the text to the right of the body (for 8 pixels to the right)
        var textPosition = this.SphereNode.CenterPosition + rightDirectionVector * (this.SphereNode.Radius + ((2 * 8) / viewWidth * xScale * lookDirectionDistance));

        // and adjust the matrix
        invertedView.M41 = textPosition.X;
        invertedView.M42 = textPosition.Y;
        invertedView.M43 = textPosition.Z;

        // Make sure that NameSceneNode has MatrixTransform
        if (NameSceneNode.Transform is not MatrixTransform matrixTransform)
        {
            matrixTransform = new MatrixTransform();
            NameSceneNode.Transform = matrixTransform;
        }

        matrixTransform.SetMatrix(Matrix4x4.CreateScale(scaleFactor, scaleFactor, 1) * invertedView);
    }
}
