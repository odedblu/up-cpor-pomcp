(define (problem medicalPKS10)
(:domain medicalPKS10)

 (:init
	(and
		(stain s0) (ndead)
		(probabilistic 
			0.1 (ill i0) 
			0.1 (ill i1) 
			0.1 (ill i2) 
			0.1 (ill i3) 
			0.1 (ill i4) 
			0.1 (ill i5) 
			0.1 (ill i6) 
			0.1 (ill i7) 
			0.1 (ill i8) 
			0.1 (ill i9) 
			0.1 (ill i10) 
		)
	)	 
 )



 (:goal (and (ill i0) (ndead)))
)