// Interface implemented by any UI element that can receive a dragged card.
// Both the human hand reorder zone and the draft staging zone implement this.
namespace Rami
{
    public interface ICardDropZone
    {
        /// <summary>Returns true when this zone will accept the dragged card.</summary>
        bool CanAcceptDrop(CardView card);

        /// <summary>Called every frame while the card is dragged over this zone.</summary>
        void OnCardDragOver(CardView card);

        /// <summary>Called when the drag ends (regardless of drop result).</summary>
        void OnCardDragEnd(CardView card);

        /// <summary>Called when the card is successfully dropped onto this zone.</summary>
        void OnCardDropped(CardView card);
    }
}
