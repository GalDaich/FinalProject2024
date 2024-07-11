const { faker } = require("@faker-js/faker/locale/en_US");
const { MongoClient } = require("mongodb");

const kidomot = ["050", "052", "053", "054", "058"];
const agesToMatchWith = ["18-25", "26-32", "33-40", "41-50", "51+"];
const yesOrNoFields = ["Yes", "No"];
const preferredAccomodation = ["Hotel", "Hostel", "B&B", "Camping"];
const citiesInIsrael = [
  "Jerusalem",
  "Tel Aviv",
  "Haifa",
  "Rishon LeZion",
  "Petah Tikva",
  "Ashdod",
  "Netanya",
  "Be'er Sheva",
  "Bnei Brak",
  "Holon",
  "Rehovot",
  "Bat Yam",
  "Ramat Gan",
  "Ashkelon",
  "Kfar Saba",
  "Modi'in-Maccabim-Re'ut",
  "Hadera",
  "Bet Shemesh",
  "Lod",
  "Nazareth",
  "Ramla",
  "Raanana",
  "Herzliya",
  "Kiryat Ata",
  "Afula",
  "Netivot",
  "Eilat",
  "Kiryat Gat",
  "Hod HaSharon",
  "Givatayim",
  "Karmiel",
  "Beit She'an",
  "Tiberias",
  "Nahariya",
  "Kiryat Bialik",
  "Ramat Hasharon",
  "Dimona",
  "Yavne",
  "Or Yehuda",
  "Zefat",
  "Kiryat Motzkin",
  "Kiryat Yam",
  "Kiryat Ono",
  "Tirat Carmel",
  "Sderot",
  "Nesher",
  "Rosh HaAyin",
  "Pardes Hanna-Karkur",
  "Migdal HaEmek",
  "Ariel",
  "Ma'alot-Tarshiha",
  "Kiryat Shmona",
  "Tzfat",
  "Arad",
  "Yokneam",
  "Safed",
  "Beit Shemesh",
  "Gedera",
  "Omer",
  "Yehud",
  "Mevo Betar",
  "Nes Ziona",
  "Kfar Yona",
  "Ganei Tikva",
];
const monthToLeave = [
  "January",
  "February",
  "March",
  "April",
  "May",
  "June",
  "July",
  "August",
  "September",
  "October",
  "November",
  "December",
];
const whereToTravel = [
  "North America - For example, United States, Canada, Mexico",
  "Central America and the Caribbean - For example, Costa Rica, Panama, Belize, Bahamas, Jamaica, Dominican Republic",
  "South America - For example, Brazil, Argentina, Chile, Peru, Colombia",
  "Western Europe - For example, France, Germany, Netherlands, Belgium, Switzerland",
  "Southern Europe - For example, Italy, Spain, Greece, Portugal",
  "Northern Europe (Scandinavia) - For example, Sweden, Norway, Denmark, Finland",
  "Eastern Europe - For example, Russia, Poland, Czech Republic, Hungary, Romania",
  "Southeast Asia - For example, Thailand, Vietnam, Indonesia, Malaysia, Philippines",
  "East Asia - For example, China, Japan, South Korea, Taiwan",
  "South Asia - For example, India, Sri Lanka, Nepal, Maldives",
  "Middle East - For example, UAE, Israel, Jordan, Egypt",
  "North Africa - For example, Morocco, Egypt, Tunisia, Algeria",
  "Central Africa - For example, Democratic Republic of the Congo, Cameroon, Gabon, Central African Republic",
  "Southern Africa - For example, South Africa, Namibia, Botswana, Zimbabwe",
  "Oceania - For example, Australia, New Zealand, Fiji, Papua New Guinea",
  "Central Asia - For example, Kazakhstan, Uzbekistan, Kyrgyzstan, Turkmenistan",
];
const typeOfTrip = [
  "Water trip",
  "Climbing trip",
  "Cultural trip",
  "Chill trip",
  "Urban trip",
  "Scenic trip",
  "Culinary trip",
  "Off-roading trip",
  "Bicycle trip",
  "Ski trip",
  "Backpacking trip",
  "Shopping trip",
];
const hobbies = [
  "Reading",
  "Writing",
  "Cooking",
  "Baking",
  "Gardening",
  "Hiking",
  "Cycling",
  "Running",
  "Swimming",
  "Fishing",
  "Knitting",
  "Sewing",
  "Painting",
  "Drawing",
  "Photography",
  "Dancing",
  "Singing",
  "Listening to music",
  "Watching movies",
  "Traveling",
  "Camping",
  "Board games",
  "Video gaming",
  "Yoga",
  "Meditation",
  "Crafting",
  "Model building",
  "Birdwatching",
];

function selectRandomItem(arr) {
  return arr[Math.floor(Math.random() * arr.length)];
}

function generateFakePhoneNumber() {
  return selectRandomItem(kidomot) + faker.phone.number("#######");
}

function generateFakePerson() {
  const dateOfBirth = faker.date.birthdate({ min: 18, max: 70, mode: "age" });
  const today = new Date();
  let age = today.getFullYear() - dateOfBirth.getFullYear();
  const monthDifference = today.getMonth() - dateOfBirth.getMonth();
  const dayDifference = today.getDate() - dateOfBirth.getDate();

  if (monthDifference < 0 || (monthDifference === 0 && dayDifference < 0)) {
    age--;
  }

  const travelInfo = {
    wantsToTravelTo: selectRandomItem(whereToTravel),
    wantsToLeaveOn: selectRandomItem(monthToLeave),
    wantedTrip: selectRandomItem(typeOfTrip),
    preferredAccomodation: selectRandomItem(preferredAccomodation),
    agesToMatchWith: selectRandomItem(agesToMatchWith),
    shomerShabbat: selectRandomItem(yesOrNoFields),
    hasDriversLicense: selectRandomItem(yesOrNoFields),
    veganOrVegetarian: selectRandomItem(yesOrNoFields),
    isSpontanious: selectRandomItem(yesOrNoFields),
    prefersNightTrips: selectRandomItem(yesOrNoFields),
  };

  return {
    personalInfo: {
      fullName: faker.person.firstName() + " " + faker.person.lastName(),
      emailAddress: faker.internet.email(),
      password: faker.internet.password({ length: 12 }),
      phoneNumber: generateFakePhoneNumber(),
      dateOfBirth: dateOfBirth,
      currentAge: age,
      livesAt: selectRandomItem(citiesInIsrael),
      additionalHobbies: Array.from({ length: 3 }, () =>
        selectRandomItem(hobbies)
      ),
    },
    travelInfo: travelInfo,
  };
}

function isAgeMatch(age, ageRange) {
  if (ageRange === "51+") {
    return age >= 51;
  } else {
    const [minAge, maxAge] = ageRange.split("-").map(Number);
    return age >= minAge && age <= maxAge;
  }
}

const uri = "mongodb://localhost:27017";
const client = new MongoClient(uri, {
  useNewUrlParser: true,
  useUnifiedTopology: true,
});

async function run() {
  try {
    await client.connect();

    const database = client.db("FPDB-UsersAndTravelPlans-1107-250k-v5");
    const usersCollection = database.collection("Users");
    const travelPlansCollection = database.collection("TravelPlans");

    const fakePeople = Array.from({ length: 250000 }, generateFakePerson);

    const filteredPeople = fakePeople.filter((person) =>
      isAgeMatch(
        person.personalInfo.currentAge,
        person.travelInfo.agesToMatchWith
      )
    );

    const personalInsertResults = await usersCollection.insertMany(
      filteredPeople.map((person) => person.personalInfo)
    );

    const travelPlans = filteredPeople.map((person, index) => ({
      personId: personalInsertResults.insertedIds[index],
      fullName: person.personalInfo.fullName,
      phoneNumber: person.personalInfo.phoneNumber,
      dateOfBirth: person.personalInfo.dateOfBirth,
      currentAge: person.personalInfo.currentAge,
      livesAt: person.personalInfo.livesAt,
      additionalHobbies: person.personalInfo.additionalHobbies,
      ...person.travelInfo,
    }));

    await travelPlansCollection.insertMany(travelPlans);

    console.log(
      `Successfully inserted ${personalInsertResults.insertedCount} personal documents and ${travelPlans.length} travel documents.`
    );
  } catch (error) {
    console.error("Error:", error);
  } finally {
    await client.close();
  }
}

run().catch(console.dir);
