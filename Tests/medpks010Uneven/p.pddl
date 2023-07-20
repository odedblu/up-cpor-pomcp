(define (problem medicalPKS10)
(:domain medicalPKS10)

 (:init 
 (probabilistic 0.1 (and (ill i0) (stain s0) (ndead))
			 0.1 (and (ill i1) (stain s0) (ndead))
			 0.1 (and (ill i2) (stain s0) (ndead))
			 0.1 (and (ill i3) (stain s0) (ndead))
			 0.1 (and (ill i4) (stain s0) (ndead))
			 0.1 (and (ill i5) (stain s0) (ndead))
			 0.1 (and (ill i6) (stain s0) (ndead))
			 0.1 (and (ill i7) (stain s0) (ndead))
			 0.1 (and (ill i8) (stain s0) (ndead))
			 0.1 (and (ill i9) (stain s0) (ndead))
			 0.1 (and (ill i10) (stain s0) (ndead))
)

)

 (:goal (and (ill i0) (ndead))))
