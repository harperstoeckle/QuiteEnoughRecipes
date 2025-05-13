using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using ReLogic.Content;
using System.Linq;
using System;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.Localization;
using Terraria.UI;
using Terraria;
using Terraria.ModLoader.UI;

namespace QuiteEnoughRecipes;

public class UIWindow : UIPanel
{
	private class UIHelpIcon : UIImageButton
	{
		private LocalizedText _text;

		public UIHelpIcon(LocalizedText helpText) : base(QERAssets.ButtonHelp)
		{
			_text = helpText;
			SetVisibility(1.0f, 0.8f);
		}

		protected override void DrawSelf(SpriteBatch sb)
		{
			base.DrawSelf(sb);
			if (IsMouseHovering)
			{
				UICommon.TooltipMouseText(_text.Value);
			}
		}
	}

	private const float BarInnerPadding = 10;
	private const float BarOuterPadding = 6;
	private const float BarItemWidth = 22;
	private const float BarItemHeight = BarItemWidth;
	private const float BarHeight = BarItemHeight + 2 * BarOuterPadding;

	/*
	 * Window resizing considers two different regions. First, the cursor is checked against each
	 * edge of the window. If it is at most `ResizeCornerWidth` from the edge, then that side is
	 * eligible to be resized. E.g., if the cursor is in the `ResizeCornerWidth × ResizeCornerWidth`
	 * region in the top-left of the window, then both the top and left side could be resized.
	 *
	 * However, the cursor must also be within the border of width `ResizeBorderWidth` around the
	 * edge of the window to actually start resizing when clicked.
	 */
	private const float ResizeCornerWidth = 30;
	private const float ResizeBorderWidth = 7;

	private struct DragState
	{
		public required Vector2 OriginalSize;
		public required Vector2 OriginalPos;
		public required Vector2 OriginalMouse;
	}

	private UIPanel _topBar = new(){
		Width = StyleDimension.Fill,
		Height = new(BarHeight, 0),
		BackgroundColor = QERColors.Brown,
		BorderColor = QERColors.DarkBrown,
	};

	/*
	 * Used to determine where to put the next item in the bar. Bar elements are arranged from left
	 * to right.
	 */
	private float _topBarOffset = 0.0f;

	// Offset on the right side. Only used for help.
	private float _topBarHelpOffset = 0.0f;

	// When not null, we assume this window is being dragged.
	private DragState? _dragState = null;

	/*
	 * Each of these is true when the corresponding window resize region is being hovered, or if
	 * we're currently dragging and the resize region was being hovered when we clicked to start
	 * dragging.
	 */
	private bool _resizeLeft = false;
	private bool _resizeRight = false;
	private bool _resizeTop = false;
	private bool _resizeBottom = false;

	private bool HoveringResize => _resizeLeft || _resizeRight || _resizeTop || _resizeBottom;

	// True if the window is being dragged or resized.
	public bool IsDragging => _dragState is not null;

	// Stuff should just be directly appended to this instead of the window itself.
	public UIElement Contents { get; private set; } = new(){
		Width = StyleDimension.Fill,
		Height = new(-BarHeight, 1),
		VAlign = 1,
	};

	public UIWindow()
	{
		BackgroundColor = QERColors.Brown * 0.7f;
		BorderColor = QERColors.DarkBrown * 0.7f;
		SetPadding(0);
		Contents.SetPadding(ResizeBorderWidth);
		_topBar.SetPadding(BarOuterPadding);

		Append(_topBar);
		Append(Contents);

		var closeButton = new UIImageButton(QERAssets.ButtonClose);
		closeButton.SetVisibility(1.0f, 0.8f);
		closeButton.OnLeftClick += (elem, evt) => PressCloseButton();

		AddElementToBar(closeButton);
	}

	// This is called when the close button is pressed.
	protected virtual void PressCloseButton() => UISystem.WindowManager?.Close(this);

	public override void LeftMouseDown(UIMouseEvent e)
	{
		bool isTargetingThisWindow = e.Target == this || e.Target == Contents || e.Target == _topBar;

		/*
		 * Hack so that when this event gets propagated to the parent `WindowManager`, it will be
		 * able to tell that it's a `UIWindow`.
		 */
		base.LeftMouseDown(new(isTargetingThisWindow ? this : e.Target, e.MousePosition));

		if (isTargetingThisWindow)
		{
			var dims = GetOuterDimensions();
			var parentBounds = GetParentDimensions();
			var relativePos = dims.Position() - parentBounds.Position();

			/*
			 * There is, in my opinion, no clear way to make percentages work with dragging in a
			 * well-behaved way, so we simply convert the dimensions exclusively into pixels once we
			 * start dragging.
			 */
			Left.Pixels = relativePos.X;
			Top.Pixels = relativePos.Y;
			Width.Pixels = dims.Width;
			Height.Pixels = dims.Height;

			Left.Percent = 0;
			Top.Percent = 0;
			Width.Percent = 0;
			Height.Percent = 0;
			HAlign = 0;
			VAlign = 0;

			var dragState = new DragState{
				OriginalSize = new Vector2(Width.Pixels, Height.Pixels),
				OriginalPos = new Vector2(Left.Pixels, Top.Pixels),
				OriginalMouse = Main.MouseScreen,
			};

			/*
			 * If we're not resizing, then we only want to drag the window if we grabbed it by the
			 * top bar.
			 */
			if (HoveringResize || _topBar.ContainsPoint(Main.MouseScreen))
			{
				_dragState = dragState;
			}
		}
	}

	public override void LeftMouseUp(UIMouseEvent e)
	{
		base.LeftMouseUp(e);
		_dragState = null;
	}

	public override void Update(GameTime t)
	{
		base.Update(t);

		if (IsMouseHovering)
		{
			PlayerInput.LockVanillaMouseScroll("QuiteEnoughRecipes/UIWindow");
			Main.LocalPlayer.mouseInterface = true;
		}
	}

	// `e` should have a fixed width of `BarItemWidth` and be smaller than `BarHeight`.
	public void AddElementToBar(UIElement e)
	{
		e.Left = new(_topBarOffset, 0);
		e.Width = new(BarItemWidth, 0);
		e.Height = new(BarItemHeight, 0);
		_topBar.Append(e);
		_topBarOffset += BarItemWidth + BarInnerPadding;
	}

	// Add a help button to the right side (that's where help is).
	public void AddHelp(LocalizedText helpText)
	{
		var helpIcon = new UIHelpIcon(helpText);
		helpIcon.HAlign = 1;
		helpIcon.Left = new(-_topBarHelpOffset, 0);
		helpIcon.Width = new(BarItemWidth, 0);
		helpIcon.Height = new(BarItemHeight, 0);
		_topBar.Append(helpIcon);
		_topBarHelpOffset += BarItemWidth + BarInnerPadding;
	}

	public CalculatedStyle GetParentDimensions()
	{
		return Parent?.GetInnerDimensions() ?? UserInterface.ActiveInstance.GetDimensions();
	}

	protected override void DrawSelf(SpriteBatch sb)
	{
		if (_dragState is DragState s)
		{
			var offset = Main.MouseScreen - s.OriginalMouse;

			var parentBounds = GetParentDimensions();
			var parentSize = new Vector2(parentBounds.Width, parentBounds.Height);

			// We're not resizing, so we're dragging the window.
			if (!HoveringResize)
			{
				Left.Pixels = Math.Clamp(s.OriginalPos.X + offset.X, 0, parentSize.X - Width.Pixels);
				Top.Pixels = Math.Clamp(s.OriginalPos.Y + offset.Y, 0, parentSize.Y - Height.Pixels);
			}
			else
			{
				if (_resizeLeft)
				{
					Left.Pixels = Math.Clamp(s.OriginalPos.X + offset.X, 0, s.OriginalPos.X + s.OriginalSize.X - MinWidth.Pixels);
					Width.Pixels = s.OriginalSize.X + s.OriginalPos.X - Left.Pixels;
				}
				else if (_resizeRight)
				{
					Width.Pixels = Math.Clamp(s.OriginalSize.X + offset.X, MinWidth.Pixels, parentSize.X - Left.Pixels);
				}

				if (_resizeTop)
				{
					Top.Pixels = Math.Clamp(s.OriginalPos.Y + offset.Y, 0, s.OriginalPos.Y + s.OriginalSize.Y - MinHeight.Pixels);
					Height.Pixels = s.OriginalSize.Y + s.OriginalPos.Y - Top.Pixels;
				}
				else if (_resizeBottom)
				{
					Height.Pixels = Math.Clamp(s.OriginalSize.Y + offset.Y, MinHeight.Pixels, parentSize.Y - Top.Pixels);
				}
			}

			Recalculate();
		}
		else
		{
			var dims = GetOuterDimensions();

			float right = dims.X + dims.Width;
			float bottom = dims.Y + dims.Height;

			_resizeLeft = Main.mouseX <= dims.X + ResizeCornerWidth;
			_resizeRight = Main.mouseX >= right - ResizeCornerWidth;
			_resizeTop = Main.mouseY <= dims.Y + ResizeCornerWidth;
			_resizeBottom = Main.mouseY >= bottom - ResizeCornerWidth;

			// Cursor is outside of the window border, so we shouldn't be resizing.
			if (dims.X + ResizeBorderWidth < Main.mouseX
					&& Main.mouseX < right - ResizeBorderWidth
					&& dims.Y + ResizeBorderWidth < Main.mouseY
					&& Main.mouseY < bottom - ResizeBorderWidth
					|| !IsMouseHovering)
			{
				_resizeLeft = _resizeRight = _resizeTop = _resizeBottom = false;
			}
		}

		// Two sides being resized at once is a corner. One is an edge.
		int numResizeDirs = ((bool[])[_resizeLeft, _resizeRight, _resizeTop, _resizeBottom])
			.Count(b => b);

		if (numResizeDirs == 2)
		{
			if (_resizeLeft && _resizeTop || _resizeBottom && _resizeRight)
			{
				UISystem.CustomCursorTexture = QERAssets.CursorCornerLeft;
			}
			else
			{
				UISystem.CustomCursorTexture = QERAssets.CursorCornerRight;
			}

			UISystem.CustomCursorOffset = UISystem.CustomCursorTexture.Frame().Size() / 2;
		}
		else if (numResizeDirs == 1)
		{
			if (_resizeLeft || _resizeRight)
			{
				UISystem.CustomCursorTexture = QERAssets.CursorEdgeHorizontal;
			}
			else
			{
				UISystem.CustomCursorTexture = QERAssets.CursorEdgeVertical;
			}

			UISystem.CustomCursorOffset = UISystem.CustomCursorTexture.Frame().Size() / 2;
		}

		base.DrawSelf(sb);
	}
}
