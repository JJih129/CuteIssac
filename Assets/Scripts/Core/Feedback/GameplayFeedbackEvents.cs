using System;

namespace CuteIssac.Core.Feedback
{
    public static class GameplayFeedbackEvents
    {
        public static event Action<FloatingFeedbackRequest> FloatingFeedbackRequested;
        public static event Action<BannerFeedbackRequest> BannerFeedbackRequested;
        public static event Action<ThreatFlashRequest> ThreatFlashRequested;

        public static void RaiseFloatingFeedback(FloatingFeedbackRequest request)
        {
            if (FloatingFeedbackRequested == null)
            {
                GameplayFeedbackBootstrap.EnsurePresenterExists();
            }

            FloatingFeedbackRequested?.Invoke(request);
        }

        public static void RaiseBannerFeedback(BannerFeedbackRequest request)
        {
            if (BannerFeedbackRequested == null)
            {
                GameplayFeedbackBootstrap.EnsurePresenterExists();
            }

            BannerFeedbackRequested?.Invoke(request);
        }

        public static void RaiseThreatFlash(ThreatFlashRequest request)
        {
            if (ThreatFlashRequested == null)
            {
                GameplayFeedbackBootstrap.EnsurePresenterExists();
            }

            ThreatFlashRequested?.Invoke(request);
        }
    }
}
