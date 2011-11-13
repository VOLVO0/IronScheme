﻿#| License
Copyright (c) 2007,2008,2009,2010,2011 Llewellyn Pritchard 
All rights reserved.
This source code is subject to terms and conditions of the BSD License.
See docs/license.txt. |#

(library (ironscheme typed)
  (export
    :
    ->
    lambda:
    let:
    define:
    letrec:)
  (import 
    (ironscheme)
    (ironscheme clr) 
    (ironscheme syntax-format))
    
  (define-syntax ->
    (lambda (x)
      (syntax-violation '-> "invalid usage of auxilliary keyword" x)))    

  (define-syntax :
    (lambda (x)
      (define (parse-type type)
        (syntax-case type (->)
          [(arg ... -> ret)
            #'((arg ...) ret)]
          [type #'type]))  
      (syntax-case x ()
        [(_ id type)
          (identifier? #'id)
          (with-syntax [(type-spec-id (syntax-format ":~a" #'id #'id))
                        (type (parse-type #'type))]
            #'(define-syntax type-spec-id
                (make-compile-time-value 'type)))])))
                
  (define-syntax define:
    (lambda (x)
      (syntax-case x ()              
        [(_ (id args ...) body)
          (with-syntax [(type-spec-id (syntax-format ":~a" #'id #'id))]
            (lambda (lookup)
              (let ((type-spec (lookup #'type-spec-id)))
                (unless type-spec
                  (syntax-violation (syntax->datum #'id) "type spec not found" x))
                (with-syntax [(type-spec (datum->syntax #'id type-spec))]
                  #'(define id
                      (typed-lambda (args ...) 
                        type-spec
                        body))))))]
        [(_ id val)
          (with-syntax [(type-spec-id (syntax-format ":~a" #'id #'id))]
            (lambda (lookup)
              (let ((type-spec (lookup #'type-spec-id)))
                (unless type-spec
                  (syntax-violation (syntax->datum #'id) "type spec not found" x))
                (with-syntax [(type-spec (datum->syntax #'id type-spec))]
                  #'(define id (clr-cast type-spec val))))))])))                

  (define-syntax lambda:
    (syntax-rules (:)
      [(_ ((id : type) ...) : ret-type b b* ...)
        (typed-lambda (id ...) 
                      ((type ...) ret-type)
                      b b* ...)]
      [(_ ((id : type) ...) b b* ...)
        (lambda: ((id : type) ...) : Object b b* ...)]))

  (define-syntax let:
    (syntax-rules (:)
      [(_ ((id : type val) ...) : ret-type b b* ...)
        ((lambda: ((id : type) ...) : ret-type b b* ...) val ...)]
      [(_ ((id : type val) ...) b b* ...)
        (let: ((id : type val) ...) : Object b b* ...)]      
      [(_ var ((id : type val) ...) : ret-type b b* ...)  
        (letrec: ((var : (IronScheme.Runtime.Typed.TypedClosure type ... ret-type) 
                    (lambda: ((id : type) ...) : ret-type b b* ...))) : ret-type
          (var val ...))]
      [(_ var ((id : type val) ...) b b* ...)
        (let: var ((id : type val) ...) : Object b b* ...)]))
                      
  (define-syntax letrec:
    (lambda (x)
      (syntax-case x (:)
        ((_ ((i : type e) ...) : ret-type b1 b2 ...)
         (with-syntax
             (((t ...) (generate-temporaries #'(i ...))))
           #'(let: ((i : type '()) ...) : ret-type
               (let: ((t : type e) ...) : ret-type
                 (set! i t) ...
                 (begin b1 b2 ...)))))))))