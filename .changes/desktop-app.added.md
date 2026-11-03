WingetUI adds a Windows desktop interface for WinGet community catalog search, installed packages, available updates, and package operation activity.
The Updates list includes row checkboxes, select-all and clear controls, and a selected package count.
Restart as administrator starts a fresh elevated session and requests silent installer mode.
Elevated sessions retry WinGet COM activation with the signed-in user registration when standard activation reports class not registered.
Update checkboxes retain independent selections and the batch uses only checked packages.
Administrator sessions run WinGet in-process so supported installers can use the session elevation.
Added per-package upgrade progress and a sortable Activity log with copyable details.
