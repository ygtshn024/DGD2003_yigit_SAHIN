GDD: School Cleaning Squad
1.(GENARAL OVERVİEW)
Genre: 3D First-Person Simulation / Casual.

Theme: Cleaning all four wings of the school before the recess bell rings.

Visual Style: Low-poly, bright, and vibrant colors. High contrast between dirty areas (grey/brown) and clean areas (shiny/white).

Platform: PC.

2.(STORY AND OBJECTİVE)
A massive food fight and a lab accident have left the school in total chaos! The principal is furious and demands the school be spotless before the next class starts. As the janitor, your goal is to use your equipment to reach 100% cleanliness in all 4 blocks (A, B, C, D).

3. Level Design (Based on the "X" Plan)
Each wing of the building features a unique type of mess:

Central Square (The Hub): The starting point. Contains the equipment station and a large dumpster for trash bags.

Block A (Offices): "Paperwork Disaster." Scattered files, tipped-over coffee mugs, and dusty desks.

Block B (Laboratories): "Chemical Spills." Glowing, sticky green and blue liquid spills on the floor.

Block C (Classrooms): "School Classics." Chewing gum under desks, messy chalkboard drawings, and paper airplanes.

Block D (Kitchen/Cafeteria): "Food Fiasco." Dropped food, dirty trays, and greasy stains.

4.CORE MECHANİCS
Interact (E): Pick up physical trash (papers, bottles, trays) and place them into trash bags.

Mopping (Left Click): Clean up liquid spills on the floor by walking over them with the mop.

Scrubbing (Right Click): Use a sponge to remove tough stains like gum or graffiti from walls and desks.

Cleanliness Bar: A UI element at the top of the screen showing the percentage of the current block cleaned (e.g., "Block B: 65% Clean").

5. DEVELOPMENT TİPS (Unity)
Technical shortcuts to finish the game quickly:

Stain Removal: Place "Plane" or "Quad" objects on the floor to represent stains. When the player's cleaning tool touches these, use gameObject.SetActive(false); to remove them.

Trash Collection: Add a Tag (e.g., "Trash") to all collectible items. When clicked, increment the inventory count and destroy the object.

Visual Feedback: When a room is 100% clean, slightly increase the Ambient Light to enhance the "clean feeling."
