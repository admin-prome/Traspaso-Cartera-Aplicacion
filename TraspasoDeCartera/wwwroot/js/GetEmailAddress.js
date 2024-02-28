export async function getEmailAdress() {
    try {
        // Make a GET request to the /.auth/me endpoint
        const response = await fetch("https://traspaso-cartera.azurewebsites.net/.auth/me", {
            method: "GET",
            headers: {
                "Content-Type": "application/json",
                // Add any additional headers as needed
            },
        });
        console.log(response);
        if (response.ok) {
            // Parse the JSON response
            const userData = await response.json();

            // Extract the email address
            const emailClaim = userData.filter(claim => claim.typ === "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress");

            if (emailClaim.length > 0) {
                return emailClaim[0].val;
            } else {
                console.error("Email address claim not found in response.");
                return null;
            }
        } else {
            console.error(`Failed to fetch email address: ${response.status} ${response.statusText}`);
            return null;
        }
    } catch (error) {
        console.error("Error fetching email address:", error);
        return null;
    }
}