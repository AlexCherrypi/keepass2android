﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using keepass2android.services.AutofillBase;
using keepass2android.services.Kp2aAutofill;

namespace keepass2android.services
{
    class Kp2aAutofillIntentBuilder : IAutofillIntentBuilder
    {
        private static int _pendingIntentRequestCode = 0;

        public PendingIntent GetAuthPendingIntentForResponse(Context context, string query, string queryDomain, string queryPackage,
            bool isManualRequest, bool autoReturnFromQuery, AutofillServiceBase.DisplayWarning warning)
        {
            Intent intent = new Intent(context, typeof(ChooseForAutofillActivity));
            intent.PutExtra(ChooseForAutofillActivityBase.ExtraQueryString, query);
            intent.PutExtra(ChooseForAutofillActivityBase.ExtraQueryDomainString, queryDomain);
            intent.PutExtra(ChooseForAutofillActivityBase.ExtraQueryPackageString, queryPackage);
            intent.PutExtra(ChooseForAutofillActivityBase.ExtraIsManualRequest, isManualRequest);
            intent.PutExtra(ChooseForAutofillActivityBase.ExtraAutoReturnFromQuery, autoReturnFromQuery);
            intent.PutExtra(ChooseForAutofillActivityBase.ExtraDisplayWarning, (int)warning);
            return PendingIntent.GetActivity(context, _pendingIntentRequestCode++, intent, Util.AddMutabilityFlag(PendingIntentFlags.CancelCurrent, PendingIntentFlags.Mutable));
        }

        public PendingIntent GetAuthPendingIntentForWarning(Context context, string query, string queryDomain, string queryPackage,
            AutofillServiceBase.DisplayWarning warning)
        {
            Intent intent = new Intent(context, typeof(ChooseForAutofillActivity));
            intent.PutExtra(ChooseForAutofillActivityBase.ExtraQueryString, query);
            intent.PutExtra(ChooseForAutofillActivityBase.ExtraQueryDomainString, queryDomain);
            intent.PutExtra(ChooseForAutofillActivityBase.ExtraQueryPackageString, queryPackage);
            intent.PutExtra(ChooseForAutofillActivityBase.ExtraDisplayWarning, (int)warning);
            intent.PutExtra(ChooseForAutofillActivityBase.ExtraUseLastOpenedEntry, true);
            return PendingIntent.GetActivity(context, _pendingIntentRequestCode++, intent, Util.AddMutabilityFlag(PendingIntentFlags.CancelCurrent, PendingIntentFlags.Immutable));
        }

        public PendingIntent GetDisablePendingIntentForResponse(Context context, string query,
            bool isManualRequest, bool isDisable)
        {
            Intent intent = new Intent(context, typeof(DisableAutofillForQueryActivity));
            intent.PutExtra(ChooseForAutofillActivityBase.ExtraQueryString, query);
            intent.PutExtra(ChooseForAutofillActivityBase.ExtraIsManualRequest, isManualRequest);
            intent.PutExtra(DisableAutofillForQueryActivity.ExtraIsDisable, isDisable);

            return PendingIntent.GetActivity(context, _pendingIntentRequestCode++, intent, Util.AddMutabilityFlag(PendingIntentFlags.CancelCurrent, PendingIntentFlags.Immutable));
        }

        public Intent GetRestartAppIntent(Context context)
        {
            var intent = new Intent(context, typeof(SelectCurrentDbActivity));
            intent.AddFlags(ActivityFlags.ForwardResult);
            return intent;
        }

        public int AppIconResource
        {
            get { return AppNames.LauncherIcon; }
        }
    }
}