(define (problem unix-3)
   (:domain unix)
   (:objects root sub1 sub2 sub11 sub12 sub21 sub22 - DIR my-file - FILE)
   
   (:init
     (sub-dir root sub1)
     (sub-dir root sub2)
     (sub-dir sub1 sub11)
     (sub-dir sub1 sub12)
     (sub-dir sub2 sub21)
     (sub-dir sub2 sub22)
     (is-cur-dir root)
     
     (oneof
        (file-in-dir my-file sub11)
        (file-in-dir my-file sub21)
        (file-in-dir my-file sub12)
        (file-in-dir my-file sub22))
     )
   (:goal (file-in-dir my-file root))
)
