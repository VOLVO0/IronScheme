﻿#| License
Copyright (c) 2007-2016 Llewellyn Pritchard
All rights reserved.
This source code is subject to terms and conditions of the BSD License.
See docs/license.txt. |#

(library (ironscheme typed)
  (export
    :
    ->
    lambda:
    case-lambda:
    let:
    let*:
    define:
    letrec:
    letrec*:)
  (import 
    (ironscheme typed core)))
