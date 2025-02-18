/*
This file is part of Keepass2Android, Copyright 2013 Philipp Crocoll. This file is based on Keepassdroid, Copyright Brian Pellin.

  Keepass2Android is free software: you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation, either version 3 of the License, or
  (at your option) any later version.

  Keepass2Android is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.

  You should have received a copy of the GNU General Public License
  along with Keepass2Android.  If not, see <http://www.gnu.org/licenses/>.
  */

using Android.App;
using Android.Content;

namespace keepass2android
{

    public class CancelDialog : Dialog
    {
        protected readonly Activity _activity;

        public CancelDialog(Activity activity) : base(activity)
        {
            _activity = activity;
        }

        public bool Canceled { get; private set; }


        public override void Cancel()
        {
            base.Cancel();
            Canceled = true;
        }
    }

}

