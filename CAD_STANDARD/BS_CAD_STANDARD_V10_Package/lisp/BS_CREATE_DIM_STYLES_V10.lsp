; BS CAD Standard V10 - Dimension style creation script
; Command: BS_CREATE_DIM_STYLES_V10
; Encoding: UTF-8 with BOM

(vl-load-com)

(defun bs:safe-setvar (var-name value / result)
  (setq result (vl-catch-all-apply 'setvar (list var-name value)))
  (not (vl-catch-all-error-p result))
)

(defun bs:set-bylayer-dim-properties (/)
  ; Line tab: all dimension object colors, linetypes and lineweights follow layer.
  (bs:safe-setvar "DIMCLRD" 256)
  (bs:safe-setvar "DIMCLRE" 256)
  (bs:safe-setvar "DIMCLRT" 256)
  (bs:safe-setvar "DIMLWD" -1)
  (bs:safe-setvar "DIMLWE" -1)
  (bs:safe-setvar "DIMLTYPE" "BYLAYER")
  (bs:safe-setvar "DIMLTEX1" "BYLAYER")
  (bs:safe-setvar "DIMLTEX2" "BYLAYER")
)

(defun bs:set-common-dim-properties (/)
  (bs:set-bylayer-dim-properties)

  ; Symbols and arrows tab.
  (bs:safe-setvar "DIMBLK" "_ARCHTICK")
  (bs:safe-setvar "DIMBLK1" "_ARCHTICK")
  (bs:safe-setvar "DIMBLK2" "_ARCHTICK")
  (bs:safe-setvar "DIMLDRBLK" "_ClosedFilled")
  (bs:safe-setvar "DIMSAH" 0)
  (bs:safe-setvar "DIMCEN" 1.0)
  (bs:safe-setvar "DIMJOGANG" 45.0)
  (bs:safe-setvar "DIMTFILL" 0)

  ; Text tab.
  (bs:safe-setvar "DIMTXSTY" "BS_TEXT_CN")
  (bs:safe-setvar "DIMTAD" 1)
  (bs:safe-setvar "DIMJUST" 0)
  (bs:safe-setvar "DIMTIH" 0)
  (bs:safe-setvar "DIMTOH" 0)
  (bs:safe-setvar "DIMGAP" 1.0)

  ; Fit tab.
  (bs:safe-setvar "DIMATFIT" 3)
  (bs:safe-setvar "DIMTMOVE" 2)
  (bs:safe-setvar "DIMTIX" 0)
  (bs:safe-setvar "DIMTOFL" 0)
  (bs:safe-setvar "DIMSOXD" 0)
  (bs:safe-setvar "DIMANNO" 0)

  ; Primary units tab.
  (bs:safe-setvar "DIMLUNIT" 2)
  (bs:safe-setvar "DIMDEC" 2)
  (bs:safe-setvar "DIMDSEP" 46)
  (bs:safe-setvar "DIMRND" 0.0)
  (bs:safe-setvar "DIMPOST" "")
  (bs:safe-setvar "DIMLFAC" 1.0)
  (bs:safe-setvar "DIMZIN" 8)
  (bs:safe-setvar "DIMAUNIT" 0)
  (bs:safe-setvar "DIMADEC" 2)
  (bs:safe-setvar "DIMAZIN" 2)
)

(defun bs:save-dim-style (style-name dim-scale text-height arrow-size dim-line-spacing ext-overrun ext-offset ext-fixed-length text-gap /)
  (bs:set-common-dim-properties)

  ; Line tab.
  (bs:safe-setvar "DIMDLE" 0.0)
  (bs:safe-setvar "DIMDLI" dim-line-spacing)
  (bs:safe-setvar "DIMEXE" ext-overrun)
  (bs:safe-setvar "DIMEXO" ext-offset)
  (bs:safe-setvar "DIMFXLON" 1)
  (bs:safe-setvar "DIMFXL" ext-fixed-length)

  ; Size and scale.
  (bs:safe-setvar "DIMTXT" text-height)
  (bs:safe-setvar "DIMASZ" arrow-size)
  (bs:safe-setvar "DIMSCALE" dim-scale)
  (bs:safe-setvar "DIMGAP" text-gap)

  (vl-cmdf "_.-DIMSTYLE" "_Save" style-name)
  (princ (strcat "\n  OK: " style-name))
)

(defun c:BS_CREATE_DIM_STYLES_V10 (/)
  (princ "\nBS CAD Standard V10: creating/updating dimension styles...")
  (if (not (tblsearch "STYLE" "BS_TEXT_CN"))
    (princ "\nWarning: text style BS_TEXT_CN does not exist. Run BS_CREATE_TEXT_STYLES_V10 first.")
  )

  ; BS_DIM_100 follows the screenshot settings.
  ; BS_DIM_50 and BS_DIM_DETAIL inherit the same style rules with their own global scale.
  (bs:save-dim-style "BS_DIM_100" 100.0 2.5 2.0 12.0 1.5 2.0 5.0 1.0)
  (bs:save-dim-style "BS_DIM_50" 50.0 2.5 2.0 12.0 1.5 2.0 5.0 1.0)
  (bs:save-dim-style "BS_DIM_DETAIL" 20.0 2.5 2.0 12.0 1.5 2.0 5.0 1.0)

  (vl-cmdf "_.-DIMSTYLE" "_Restore" "BS_DIM_100")
  (princ "\nBS CAD Standard V10: dimension styles created/updated successfully.")
  (princ)
)

(princ "\nCommand loaded: BS_CREATE_DIM_STYLES_V10")
(princ)
