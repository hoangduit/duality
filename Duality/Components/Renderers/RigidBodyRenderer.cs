﻿using System;
using System.Collections.Generic;
using System.Linq;

using Duality.Drawing;
using Duality.Editor;
using Duality.Resources;
using Duality.Cloning;
using Duality.Components.Physics;
using Duality.Properties;

namespace Duality.Components.Renderers
{
	/// <summary>
	/// A <see cref="Duality.Component"/> that renders a RigidBodies shape and outline.
	/// </summary>
	[RequiredComponent(typeof(RigidBody))]
	[EditorHintCategory(typeof(CoreRes), CoreResNames.CategoryGraphics)]
	[EditorHintImage(typeof(CoreRes), CoreResNames.ImageRigidBodyRenderer)]
	public class RigidBodyRenderer : Renderer
	{
		private	ContentRef<Material>	areaMaterial			= Material.Checkerboard;
		private	ContentRef<Material>	outlineMaterial			= Material.SolidWhite;
		private	BatchInfo				customAreaMaterial		= null;
		private	BatchInfo				customOutlineMaterial	= null;
		private	ColorRgba				colorTint				= ColorRgba.White;
		private	float					outlineWidth			= 3.0f;
		private	int						offset					= 0;
		private	bool					fillHollowShapes		= false;
		private	bool					wrapTexture				= true;

		[DontSerialize] private	CanvasBuffer	vertexBuffer	= new CanvasBuffer();


		public override float BoundRadius
		{
			get { return this.GameObj.RigidBody.BoundRadius; }
		}
		/// <summary>
		/// [GET / SET] The <see cref="Duality.Resources.Material"/> that is used for rendering the RigidBodies shape areaa.
		/// </summary>
		public ContentRef<Material> AreaMaterial
		{
			get { return this.areaMaterial; }
			set { this.areaMaterial = value; }
		}
		/// <summary>
		/// [GET / SET] The <see cref="Duality.Resources.Material"/> that is used for rendering the RigidBodies shape outlines.
		/// </summary>
		public ContentRef<Material> OutlineMaterial
		{
			get { return this.outlineMaterial; }
			set { this.outlineMaterial = value; }
		}
		/// <summary>
		/// [GET / SET] A custom, local <see cref="Duality.Resources.BatchInfo"/> overriding the <see cref="AreaMaterial"/>.
		/// </summary>
		public BatchInfo CustomAreaMaterial
		{
			get { return this.customAreaMaterial; }
			set { this.customAreaMaterial = value; }
		}
		/// <summary>
		/// [GET / SET] A custom, local <see cref="Duality.Resources.BatchInfo"/> overriding the <see cref="OutlineMaterial"/>.
		/// </summary>
		public BatchInfo CustomOutlineMaterial
		{
			get { return this.customOutlineMaterial; }
			set { this.customOutlineMaterial = value; }
		}
		/// <summary>
		/// [GET / SET] A color by which the rendered debug output is tinted.
		/// </summary>
		public ColorRgba ColorTint
		{
			get { return this.colorTint; }
			set { this.colorTint = value; }
		}
		/// <summary>
		/// [GET / SET] A virtual Z offset that affects the order in which objects are drawn. If you want to assure an object is drawn after another one,
		/// just assign a higher Offset value to the background object.
		/// </summary>
		public int Offset
		{
			get { return this.offset; }
			set { this.offset = value; }
		}
		/// <summary>
		/// [GET / SET] Specifies the width of the RigidBody outline when rendering. 
		/// No outline will be rendered, if this value is smaller than or equal zero.
		/// </summary>
		[EditorHintRange(0.0f, 100.0f)]
		public float OutlineWidth
		{
			get { return this.outlineWidth; }
			set { this.outlineWidth = value; }
		}
		/// <summary>
		/// [GET / SET] Whether or not hollow shapes like the <see cref="LoopShapeInfo"/> will be filled as if they were in fact solid.
		/// </summary>
		public bool FillHollowShapes
		{
			get { return this.fillHollowShapes; }
			set { this.fillHollowShapes = value; }
		}
		/// <summary>
		/// [GET / SET] Specifies, whether or not texture wrapping will be active when rendering the RigidBody area and outline.
		/// </summary>
		public bool WrapTexture
		{
			get { return this.wrapTexture; }
			set { this.wrapTexture = value; }
		}
		/// <summary>
		/// [GET] The internal Z-Offset added to the renderers vertices based on its <see cref="Offset"/> value.
		/// </summary>
		[EditorHintFlags(MemberFlags.Invisible)]
		public float VertexZOffset
		{
			get { return this.offset * 0.01f; }
		}


		public override void Draw(IDrawDevice device)
		{
			Transform tranform = this.GameObj.Transform;
			RigidBody body = this.GameObj.RigidBody;

			Canvas canvas = new Canvas(device, this.vertexBuffer);

			// Draw Shape Areas
			canvas.State.ZOffset = this.offset;
			if (this.customAreaMaterial != null)
				canvas.State.SetMaterial(this.customAreaMaterial);
			else
				canvas.State.SetMaterial(this.areaMaterial);
			foreach (ShapeInfo shape in body.Shapes)
			{
				if (!shape.IsValid)
					canvas.State.ColorTint = this.colorTint * ColorRgba.Red;
				else
					canvas.State.ColorTint = this.colorTint;
				this.DrawShapeArea(canvas, tranform, shape);
			}

			// Draw Shape Outlines
			if (this.outlineWidth > 0.0f)
			{
				canvas.State.ZOffset = this.offset - 1;
				if (this.customOutlineMaterial != null)
					canvas.State.SetMaterial(this.customOutlineMaterial);
				else
					canvas.State.SetMaterial(this.outlineMaterial);
				foreach (ShapeInfo shape in body.Shapes)
				{
					if (!shape.IsValid)
						canvas.State.ColorTint = this.colorTint * ColorRgba.Red;
					else
						canvas.State.ColorTint = this.colorTint;
					this.DrawShapeOutline(canvas, tranform, shape);
				}
			}
		}

		private void DrawShapeArea(Canvas canvas, Transform transform, ShapeInfo shape)
		{
			canvas.PushState();
			if		(shape is CircleShapeInfo)	this.DrawShapeArea(canvas, transform, shape as CircleShapeInfo);
			else if (shape is PolyShapeInfo)	this.DrawShapeArea(canvas, transform, shape as PolyShapeInfo);
			else if (shape is LoopShapeInfo)	this.DrawShapeArea(canvas, transform, shape as LoopShapeInfo);
			canvas.PopState();
		}
		private void DrawShapeArea(Canvas canvas, Transform transform, CircleShapeInfo shape)
		{
			Vector3 pos = transform.Pos;
			float angle = transform.Angle;
			float scale = transform.Scale;

			if (this.wrapTexture)
			{
				canvas.State.TextureCoordinateRect = new Rect(
					shape.Radius * 2.0f / canvas.State.TextureBaseSize.X,
					shape.Radius * 2.0f / canvas.State.TextureBaseSize.Y);
			}
			canvas.State.TransformScale = new Vector2(scale, scale);
			canvas.State.TransformAngle = angle;
			canvas.State.TransformHandle = -shape.Position;
			canvas.FillCircle(
				pos.X, 
				pos.Y, 
				pos.Z, 
				shape.Radius);
		}
		private void DrawShapeArea(Canvas canvas, Transform transform, PolyShapeInfo shape)
		{
			Vector3 pos = transform.Pos;
			float angle = transform.Angle;
			float scale = transform.Scale;

			if (this.wrapTexture)
			{
				Rect pointBoundingRect = shape.Vertices.BoundingBox();
				canvas.State.TextureCoordinateRect = new Rect(
					pointBoundingRect.W / canvas.State.TextureBaseSize.X,
					pointBoundingRect.H / canvas.State.TextureBaseSize.Y);
			}
			canvas.State.TransformAngle = angle;
			canvas.State.TransformScale = new Vector2(scale, scale);
			canvas.FillPolygon(shape.Vertices, pos.X, pos.Y, pos.Z);
		}
		private void DrawShapeArea(Canvas canvas, Transform transform, LoopShapeInfo shape)
		{
			// LoopShapes don't have an area. Do nothing here, unless explicitly specified
			if (!this.fillHollowShapes) return;

			Vector3 pos = transform.Pos;
			float angle = transform.Angle;
			float scale = transform.Scale;

			if (this.wrapTexture)
			{
				Rect pointBoundingRect = shape.Vertices.BoundingBox();
				canvas.State.TextureCoordinateRect = new Rect(
					pointBoundingRect.W / canvas.State.TextureBaseSize.X,
					pointBoundingRect.H / canvas.State.TextureBaseSize.Y);
			}
			canvas.State.TransformAngle = angle;
			canvas.State.TransformScale = new Vector2(scale, scale);
			canvas.FillPolygon(shape.Vertices, pos.X, pos.Y, pos.Z);
		}

		private void DrawShapeOutline(Canvas canvas, Transform transform, ShapeInfo shape)
		{
			canvas.PushState();
			if		(shape is CircleShapeInfo)	this.DrawShapeOutline(canvas, transform, shape as CircleShapeInfo);
			else if (shape is PolyShapeInfo)	this.DrawShapeOutline(canvas, transform, shape as PolyShapeInfo);
			else if (shape is LoopShapeInfo)	this.DrawShapeOutline(canvas, transform, shape as LoopShapeInfo);
			canvas.PopState();
		}
		private void DrawShapeOutline(Canvas canvas, Transform transform, CircleShapeInfo shape)
		{
			Vector3 pos = transform.Pos;
			float angle = transform.Angle;
			float scale = transform.Scale;

			if (this.wrapTexture)
			{
				canvas.State.TextureCoordinateRect = new Rect(
					shape.Radius * 2.0f / canvas.State.TextureBaseSize.X,
					shape.Radius * 2.0f / canvas.State.TextureBaseSize.Y);
			}
			canvas.State.TransformScale = new Vector2(scale, scale);
			canvas.State.TransformAngle = angle;
			canvas.State.TransformHandle = -shape.Position;
			canvas.FillCircleSegment(
				pos.X, 
				pos.Y, 
				pos.Z, 
				shape.Radius,
				0.0f,
				MathF.RadAngle360,
				this.outlineWidth);
		}
		private void DrawShapeOutline(Canvas canvas, Transform transform, PolyShapeInfo shape)
		{
			Vector3 pos = transform.Pos;
			float angle = transform.Angle;
			float scale = transform.Scale;

			if (this.wrapTexture)
			{
				Rect pointBoundingRect = shape.Vertices.BoundingBox();
				canvas.State.TextureCoordinateRect = new Rect(
					pointBoundingRect.W / canvas.State.TextureBaseSize.X,
					pointBoundingRect.H / canvas.State.TextureBaseSize.Y);
			}
			canvas.State.TransformAngle = angle;
			canvas.State.TransformScale = new Vector2(scale, scale);
			canvas.FillPolygonOutline(shape.Vertices, this.outlineWidth, pos.X, pos.Y, pos.Z);
		}
		private void DrawShapeOutline(Canvas canvas, Transform transform, LoopShapeInfo shape)
		{
			Vector3 pos = transform.Pos;
			float angle = transform.Angle;
			float scale = transform.Scale;

			if (this.wrapTexture)
			{
				Rect pointBoundingRect = shape.Vertices.BoundingBox();
				canvas.State.TextureCoordinateRect = new Rect(
					pointBoundingRect.W / canvas.State.TextureBaseSize.X,
					pointBoundingRect.H / canvas.State.TextureBaseSize.Y);
			}
			canvas.State.TransformAngle = angle;
			canvas.State.TransformScale = new Vector2(scale, scale);
			canvas.FillPolygonOutline(shape.Vertices, this.outlineWidth, pos.X, pos.Y, pos.Z);
		}
	}
}
