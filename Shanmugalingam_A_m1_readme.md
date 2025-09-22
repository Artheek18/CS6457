Artheeckumarran Shanmugalingam
ashanmug6@gatech.edu
ashanmug6

Scene Name:Demo

For this build, the animations for 4 characters 
were being modified. For the first character,
SomeDudeNoRootMotion, the turning animations
were bypassed programmatically by commenting 
anim.SetFloat("velx", inputTurn) in the 
BasicControlScript. For the SomeDudeRootMotion 
character, the forward blend tree was expanded to 
include run forward and run-while-turning clips allowing
the character to run even while turning. 3 public 
variables were added to adjust animation speed, 
root motion scale and rotations to make the character 
look natural. The button-press interaction was
also improved. Using Unity's Animator.MatchTarget()
method and Inverse kinematics, the character can 
press the red balloon when he is around the
blue circle. The animations should look fluid and
natural. Lastly, for the MinionRootMotion, the 
minion's walking is replaced by comedic steps
where the minions are jumping after every step. 
The minion also make squeaky footsteps as well. 
This is done using animation events. This is what 
should be noticed for my build.