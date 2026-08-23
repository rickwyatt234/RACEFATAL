/*
    CHARACTER FACTORY
    CREATES NEW PLAYER CHARACTERS AND THEIR ASSOCIATED DATA OBJECTS.
*/
using System;

namespace RaceFatal.Career
{
    public class CharacterFactory
    {
        public RacerState CreateNewPlayerCharacter( 
             string teamId,
            string racerName)
        {
           var racerId = Guid.NewGuid().ToString("N");
           
           return new RacerState(
                racerId: racerId,
                name: racerName,
                teamId: teamId,
                isPlayerCharacter: true);
        }
    }   
}
