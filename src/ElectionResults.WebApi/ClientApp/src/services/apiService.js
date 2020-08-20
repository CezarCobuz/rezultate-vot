let base_URL = "https://localhost:5001"

export const getVoterTurnoutUrl = (electionId) => `${base_URL}/api/results/voter-turnout?electionId=${electionId}`;

export const getElectionConfigUrl = () => `${base_URL}/api/admin/election-config`;

export const getElectionResultsUrl = (electionId, source, county) => (!source)
    ? `${base_URL}/api/results?electionId=${electionId}`
    : `${base_URL}/api/results?electionId=${electionId}&source=${source}&county=${county || ''}`;

export const getVoteMonitoringUrl = (electionId) => `${base_URL}/api/results/monitoring?electionId=${electionId}`;

export const getAuthenticationUrl = () => `${base_URL}/api/admin/login`;
export const authentication = {
    login: async (email, password) => {
        const response = await fetch(getAuthenticationUrl(), {
            method: 'POST', // *GET, POST, PUT, DELETE, etc.
            mode: 'cors', // no-cors, *cors, same-origin
            cache: 'no-cache', // *default, no-cache, reload, force-cache, only-if-cached
            credentials: 'same-origin', // include, *same-origin, omit
            headers: {
                'Content-Type': 'application/json'
                // 'Content-Type': 'application/x-www-form-urlencoded',
            },
            redirect: 'follow', // manual, *follow, error
            referrerPolicy: 'no-referrer', // no-referrer, *no-referrer-when-downgrade, origin, origin-when-cross-origin, same-origin, strict-origin, strict-origin-when-cross-origin, unsafe-url
            body: JSON.stringify({email: email, password: password}) // body data type must match "Content-Type" header
        });
       
        return response.json();
    }
};
