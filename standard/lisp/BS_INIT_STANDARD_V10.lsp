; BS CAD Standard V10 - One command initializer
; Command: BS_INIT_STANDARD_V10
; Encoding: UTF-8 with BOM

(vl-load-com)

(setq *bs-v10-lisp-dir* "D:\\01_DesignProjects\\BS_CAD_STANDARD_V10_Package\\lisp")

(defun bs:v10-load-file (file-name / full-path result)
  (setq full-path (strcat *bs-v10-lisp-dir* "\\" file-name))
  (if (findfile full-path)
    (progn
      (setq result (vl-catch-all-apply 'load (list full-path)))
      (if (vl-catch-all-error-p result)
        (princ (strcat "\nERROR loading " file-name ": " (vl-catch-all-error-message result)))
        (princ (strcat "\nLoaded: " file-name))
      )
    )
    (princ (strcat "\nERROR: cannot find " full-path))
  )
)

(defun bs:v10-safe-setvar (var-name value / result)
  (setq result (vl-catch-all-apply 'setvar (list var-name value)))
  (if (vl-catch-all-error-p result)
    (princ (strcat "\nWarning: cannot set " var-name))
  )
)

(defun bs:v10-set-units (/)
  ; Format > Units:
  ; Length type = Decimal, precision = 0.00
  ; Angle type = Decimal Degrees, precision = 0
  ; Insertion scale = Millimeters
  (bs:v10-safe-setvar "LUNITS" 2)
  (bs:v10-safe-setvar "LUPREC" 2)
  (bs:v10-safe-setvar "AUNITS" 0)
  (bs:v10-safe-setvar "AUPREC" 0)
  (bs:v10-safe-setvar "INSUNITS" 4)
  (bs:v10-safe-setvar "MEASUREMENT" 1)
  (bs:v10-safe-setvar "LTSCALE" 1.0)
  (bs:v10-safe-setvar "CELTSCALE" 1.0)
  (princ "\nDrawing units set: Decimal 0.00, insertion scale Millimeters.")
)

(defun bs:v10-try-put (object property value / result)
  (setq result (vl-catch-all-apply property (list object value)))
  (not (vl-catch-all-error-p result))
)

(defun bs:v10-try-call (method args / result)
  (setq result (vl-catch-all-apply method args))
  (not (vl-catch-all-error-p result))
)

(defun bs:v10-set-plot-settings (/ doc layout lower-left upper-right media-options media-name media-set)
  ; Model plot defaults:
  ; Printer = AutoCAD PDF (High Quality Print).pc3
  ; Paper = ISO A3, landscape
  ; Plot area = Window, from 0,0 to 420,297
  ; Scale = Fit to paper, centered
  ; CTB = BS_CAD_STANDARD_V10.ctb
  ; Quality = Highest, plot object lineweights and plot styles
  (setq doc (vla-get-ActiveDocument (vlax-get-acad-object)))
  (setq layout (vla-get-ActiveLayout doc))

  (bs:v10-try-put layout 'vla-put-ConfigName "AutoCAD PDF (High Quality Print).pc3")
  (bs:v10-try-call 'vla-RefreshPlotDeviceInfo (list layout))

  (setq media-options
    '(
      "ISO_A3_(420.00_x_297.00_MM)"
      "ISO_A3_(297.00_x_420.00_MM)"
      "ISO_full_bleed_A3_(420.00_x_297.00_MM)"
      "ISO_full_bleed_A3_(297.00_x_420.00_MM)"
    )
  )
  (setq media-set nil)
  (foreach media-name media-options
    (if (and (not media-set) (bs:v10-try-put layout 'vla-put-CanonicalMediaName media-name))
      (setq media-set T)
    )
  )

  (setq lower-left (vlax-make-safearray vlax-vbDouble '(0 . 1)))
  (vlax-safearray-fill lower-left '(0.0 0.0))
  (setq upper-right (vlax-make-safearray vlax-vbDouble '(0 . 1)))
  (vlax-safearray-fill upper-right '(420.0 297.0))
  (bs:v10-try-call 'vla-SetWindowToPlot (list layout lower-left upper-right))

  (bs:v10-try-put layout 'vla-put-PlotType 4)
  (bs:v10-try-put layout 'vla-put-UseStandardScale :vlax-true)
  (bs:v10-try-put layout 'vla-put-StandardScale 0)
  (bs:v10-try-put layout 'vla-put-CenterPlot :vlax-true)
  (bs:v10-try-put layout 'vla-put-PlotRotation 1)
  (bs:v10-try-put layout 'vla-put-PaperUnits 1)
  (bs:v10-try-put layout 'vla-put-StyleSheet "BS_CAD_STANDARD_V10.ctb")
  (bs:v10-try-put layout 'vla-put-PlotWithPlotStyles :vlax-true)
  (bs:v10-try-put layout 'vla-put-PlotWithLineweights :vlax-true)
  (bs:v10-try-put layout 'vla-put-ScaleLineweights :vlax-false)
  (bs:v10-try-put layout 'vla-put-ShowPlotStyles :vlax-false)

  (bs:v10-safe-setvar "BACKGROUNDPLOT" 0)
  (princ "\nPlot settings set: AutoCAD PDF High Quality, A3 landscape, window, fit, centered, BS_CAD_STANDARD_V10.ctb.")
)

(defun c:BS_INIT_STANDARD_V10 (/)
  (princ "\nBS CAD Standard V10: initializing drawing...")

  (bs:v10-load-file "BS_CREATE_LAYERS_V10.lsp")
  (bs:v10-load-file "BS_CREATE_TEXT_STYLES_V10.lsp")
  (bs:v10-load-file "BS_CREATE_DIM_STYLES_V10.lsp")

  (bs:v10-set-units)
  (bs:v10-set-plot-settings)

  (if (not (vl-catch-all-error-p (vl-catch-all-apply 'c:BS_CREATE_LAYERS_V10 '())))
    (princ "\nLayer step finished.")
    (princ "\nERROR: layer step failed.")
  )
  (if (not (vl-catch-all-error-p (vl-catch-all-apply 'c:BS_CREATE_TEXT_STYLES_V10 '())))
    (princ "\nText style step finished.")
    (princ "\nERROR: text style step failed.")
  )
  (if (not (vl-catch-all-error-p (vl-catch-all-apply 'c:BS_CREATE_DIM_STYLES_V10 '())))
    (princ "\nDimension style step finished.")
    (princ "\nERROR: dimension style step failed.")
  )

  (if (tblsearch "LAYER" "17-AN-临时对象")
    (bs:v10-safe-setvar "CLAYER" "17-AN-临时对象")
    (princ "\nWarning: default layer not found: 17-AN-临时对象")
  )
  (if (tblsearch "STYLE" "BS_TEXT_CN")
    (bs:v10-safe-setvar "TEXTSTYLE" "BS_TEXT_CN")
    (princ "\nWarning: default text style not found: BS_TEXT_CN")
  )
  (if (tblsearch "DIMSTYLE" "BS_DIM_100")
    (vl-cmdf "_.-DIMSTYLE" "_Restore" "BS_DIM_100")
    (princ "\nWarning: default dimension style not found: BS_DIM_100")
  )

  (bs:v10-safe-setvar "CECOLOR" "BYLAYER")
  (bs:v10-safe-setvar "CELTYPE" "BYLAYER")
  (bs:v10-safe-setvar "CELWEIGHT" -1)

  (princ "\nBS CAD Standard V10 initialization complete.")
  (princ "\nNext: set page setup to BS_CAD_STANDARD_V10.ctb, then SAVEAS DWT:")
  (princ "\nD:\\01_DesignProjects\\BS_CAD_STANDARD_V10_Package\\templates\\BS_CAD_STANDARD_V10.dwt")
  (princ)
)

(princ "\nCommand loaded: BS_INIT_STANDARD_V10")
(princ)
