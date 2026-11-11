using System.Threading.Tasks;
using UnityEngine;
using PM.horizOn.Cloud.Examples.Features;
using PM.horizOn.Cloud.Objects.Network.Responses;

public class MarketplaceDemo : MonoBehaviour
{
    // Import the SDK Examples sample. Bind its screen to the signed-in player
    // and set the stable projectId in the Inspector. Never use an Account Key.
    public MarketplaceExample screen;

    public Task<MarketplaceResult<MarketplaceListingView>> ReadListing(string listingId)
        => MarketplaceQuickstart.ReadOneListing(screen, listingId);

    // Wire only to the player's Buy confirmation after displaying ReadListing.
    public Task<bool> ConfirmPurchase(string listingId)
        => MarketplaceQuickstart.BuyReviewedListing(screen, listingId);

    // false/timeout does not prove nonexecution. Use screen.RetryPending() or
    // screen.RecoverOriginalOperation(); never replace the unresolved original.
    // The sample refreshes Inventory, Values and Marketplace before acknowledgement.
    // Its native file journal rejects WebGL writes; use a durable browser persister.
}