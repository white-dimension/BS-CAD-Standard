; BS CAD Standard 1.0 - Text style creation script
; Command: BS_CREATE_TEXT_STYLES_1_0
; Encoding: UTF-8 with BOM

(vl-load-com)

(defun bs:safe-put (object property value / result)
  (setq result (vl-catch-all-apply property (list object value)))
  (not (vl-catch-all-error-p result))
)

(defun bs:set-ttf-font (style font-family / result)
  ; TrueType fonts should be assigned by family name with SetFont.
  ; This keeps the font name visible in AutoCAD's STYLE dialog.
  (setq result
    (vl-catch-all-apply
      'vla-SetFont
      (list style font-family :vlax-false :vlax-false 0 0)
    )
  )
  (not (vl-catch-all-error-p result))
)

(defun bs:create-or-update-text-style (style-name font-family text-height width-factor oblique-angle purpose / doc styles style ok)
  (setq doc (vla-get-ActiveDocument (vlax-get-acad-object)))
  (setq styles (vla-get-TextStyles doc))
  (if (tblsearch "STYLE" style-name)
    (setq style (vla-Item styles style-name))
    (setq style (vla-Add styles style-name))
  )
  (setq ok (bs:set-ttf-font style font-family))
  (bs:safe-put style 'vla-put-Height text-height)
  (bs:safe-put style 'vla-put-Width width-factor)
  (bs:safe-put style 'vla-put-ObliqueAngle oblique-angle)
  (if ok
    (princ (strcat "\n  OK: " style-name "  Font=" font-family "  Use=" purpose))
    (princ (strcat "\n  Warning: " style-name " font may be unavailable: " font-family))
  )
)

(defun c:BS_CREATE_TEXT_STYLES_1_0 (/)
  (princ "\nBS CAD Standard 1.0: creating/updating text styles...")
  (bs:create-or-update-text-style "BS_TEXT_CN" "SimHei" 0.0 1.0 0.0 "Chinese notes")
  (bs:create-or-update-text-style "BS_TEXT_EN" "Arial" 0.0 1.0 0.0 "English, numbers, codes")
  (bs:create-or-update-text-style "BS_TEXT_TITLE" "SimHei" 0.0 1.0 0.0 "Drawing titles, area names")
  (bs:create-or-update-text-style "BS_TEXT_TABLE" "SimHei" 0.0 0.9 0.0 "Tables, title blocks, schedules")
  (bs:create-or-update-text-style "BS_TEXT_NOTE" "SimHei" 0.0 0.85 0.0 "Small notes")
  (setvar "TEXTSTYLE" "BS_TEXT_CN")
  (princ "\nBS CAD Standard 1.0: text styles created/updated successfully.")
  (princ)
)

(princ "\nCommand loaded: BS_CREATE_TEXT_STYLES_1_0")
(princ)

