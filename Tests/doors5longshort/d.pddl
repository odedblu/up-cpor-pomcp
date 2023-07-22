
(define (domain doors) 

   (:requirements :strips :typing :contingent :conditional-effects :probabilistic-effects)
   (:types pos )
   (:predicates (adj ?i ?j) (at ?i)  (opened ?i)  (problematic ?i))

   (:action sense-door
      :parameters (?i - pos ?j - pos )
      :precondition   (and (at ?i) (adj ?i ?j))
      :observe (opened ?j) )

   (:action regular-move
      :parameters (?i - pos ?j - pos )
      :precondition (and (adj ?i ?j) (at ?i) (opened ?j))
      :effect  (and (not (at ?i)) (at ?j))
      )
	  
	(:action try-open-door
      :parameters (?i - pos ?j - pos )
      :precondition (and (adj ?i ?j) (at ?i) (problematic ?j))
      :effect  (probabilistic 0.7 (and (opened ?j))))
	 )
)

