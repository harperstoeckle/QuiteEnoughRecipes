using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using Terraria;

namespace QuiteEnoughRecipes;

public class UIDraggableItem : UIItemIcon, IWindowManagerElement
{
	public bool WantsClose { get; set; } = false;
	public bool WantsMoveToFront { get; set; } = false;
	public DragRequestState WantsDrag { get; set; } = DragRequestState.None;

	public UIDraggableItem(Item i) : base(i, false)
	{
		IgnoresMouseInteraction = true;
	}

	public void OnOpen()
	{
		SetPosToMouse();
		this.StartDragging();
	}

	public void OnWindowManagerLeftMouseUp(UIMouseEvent e) => this.Close();

	public override void Update(GameTime t)
	{
		base.Update(t);
		SetPosToMouse();
	}

	private void SetPosToMouse()
	{
		Left.Pixels = Main.mouseX - Width.Pixels / 2;
		Top.Pixels = Main.mouseY - Height.Pixels / 2;
		Recalculate();
	}
}
